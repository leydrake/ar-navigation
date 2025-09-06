// Analytics tracking system
class AnalyticsTracker {
    constructor() {
        this.db = null;
        this.isInitialized = false;
        this.sessionId = this.generateSessionId();
        this.visitorId = this.getOrCreateVisitorId();
        this.startTime = Date.now();
        this.pageViews = 0;
        this.isAdminPage = window.location.pathname.includes('/admin/');
        
        this.init();
    }

    // Initialize Firebase and start tracking
    async init() {
        try {
            // Initialize Firebase if not already done
            if (typeof firebase === 'undefined') {
                console.warn('Firebase not loaded. Analytics will not work.');
                return;
            }

            if (!firebase.apps.length) {
                const firebaseConfig = {
                    apiKey: "AIzaSyB8Xi8J7t3wRSy1TeIxiGFz-Is6U0zDFVg",
                    authDomain: "navigatecampus.firebaseapp.com",
                    projectId: "navigatecampus",
                    storageBucket: "navigatecampus.appspot.com",
                    messagingSenderId: "55012323145",
                    appId: "1:55012323145:web:3408681d5a450f05b2b498",
                    measurementId: "G-39WFZN3VPV"
                };
                firebase.initializeApp(firebaseConfig);
            }

            this.db = firebase.firestore();
            this.isInitialized = true;

            // Start tracking
            this.trackPageView();
            this.setupEventListeners();
            this.trackSessionStart();

        } catch (error) {
            console.error('Error initializing analytics:', error);
        }
    }

    // Generate unique session ID
    generateSessionId() {
        return 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    // Get or create visitor ID
    getOrCreateVisitorId() {
        let visitorId = localStorage.getItem('analytics_visitor_id');
        if (!visitorId) {
            visitorId = 'visitor_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('analytics_visitor_id', visitorId);
        }
        return visitorId;
    }

    // Track page view
    async trackPageView() {
        if (!this.isInitialized) return;

        try {
            const pageData = {
                url: window.location.href,
                path: window.location.pathname,
                title: document.title,
                referrer: document.referrer || 'direct',
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                sessionId: this.sessionId,
                visitorId: this.visitorId,
                userAgent: navigator.userAgent,
                screenResolution: `${screen.width}x${screen.height}`,
                viewportSize: `${window.innerWidth}x${window.innerHeight}`,
                language: navigator.language,
                timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
                isAdminPage: this.isAdminPage
            };

            // Increment page views counter
            this.pageViews++;

            // Store page view data
            await this.db.collection('analytics').doc('page_views').collection('views').add(pageData);

            // Update page views counter
            await this.updateCounter('page_views');

            // Update daily visitors
            await this.updateDailyVisitors();

            // Update unique visitors
            await this.updateUniqueVisitors();

            // Update total visitors
            await this.updateTotalVisitors();

            // Track page-specific views
            await this.trackPageSpecificView(pageData.path);

            // Add to recent activity
            await this.addRecentActivity(`Page viewed: ${pageData.title}`);

            console.log('Page view tracked:', pageData.path);

        } catch (error) {
            console.error('Error tracking page view:', error);
        }
    }

    // Track session start
    async trackSessionStart() {
        if (!this.isInitialized) return;

        try {
            const sessionData = {
                visitorId: this.visitorId,
                startTime: firebase.firestore.FieldValue.serverTimestamp(),
                userAgent: navigator.userAgent,
                referrer: document.referrer || 'direct',
                isAdminPage: this.isAdminPage
            };

            await this.db.collection('analytics').doc('sessions').collection('active').doc(this.sessionId).set(sessionData);

            // Track session end when page unloads
            window.addEventListener('beforeunload', () => {
                this.trackSessionEnd();
            });

        } catch (error) {
            console.error('Error tracking session start:', error);
        }
    }

    // Track session end
    async trackSessionEnd() {
        if (!this.isInitialized) return;

        try {
            const sessionDuration = Date.now() - this.startTime;
            
            await this.db.collection('analytics').doc('sessions').collection('active').doc(this.sessionId).update({
                endTime: firebase.firestore.FieldValue.serverTimestamp(),
                duration: sessionDuration,
                pageViews: this.pageViews
            });

            // Move to completed sessions
            const sessionDoc = await this.db.collection('analytics').doc('sessions').collection('active').doc(this.sessionId).get();
            if (sessionDoc.exists) {
                await this.db.collection('analytics').doc('sessions').collection('completed').add(sessionDoc.data());
                await this.db.collection('analytics').doc('sessions').collection('active').doc(this.sessionId).delete();
            }

        } catch (error) {
            console.error('Error tracking session end:', error);
        }
    }

    // Update counter
    async updateCounter(counterName) {
        try {
            const counterRef = this.db.collection('analytics').doc(counterName);
            await counterRef.set({
                count: firebase.firestore.FieldValue.increment(1),
                lastUpdated: firebase.firestore.FieldValue.serverTimestamp()
            }, { merge: true });
        } catch (error) {
            console.error(`Error updating counter ${counterName}:`, error);
        }
    }

    // Update daily visitors
    async updateDailyVisitors() {
        try {
            const today = new Date().toISOString().split('T')[0];
            const dailyRef = this.db.collection('analytics').doc('daily_visitors').collection('days').doc(today);
            
            await dailyRef.set({
                count: firebase.firestore.FieldValue.increment(1),
                date: today,
                lastUpdated: firebase.firestore.FieldValue.serverTimestamp()
            }, { merge: true });
        } catch (error) {
            console.error('Error updating daily visitors:', error);
        }
    }

    // Update unique visitors
    async updateUniqueVisitors() {
        try {
            const visitorRef = this.db.collection('analytics').doc('unique_visitors').collection('visitors').doc(this.visitorId);
            
            await visitorRef.set({
                visitorId: this.visitorId,
                firstVisit: firebase.firestore.FieldValue.serverTimestamp(),
                lastVisit: firebase.firestore.FieldValue.serverTimestamp(),
                totalVisits: firebase.firestore.FieldValue.increment(1),
                isAdmin: this.isAdminPage
            }, { merge: true });
        } catch (error) {
            console.error('Error updating unique visitors:', error);
        }
    }

    // Update total visitors
    async updateTotalVisitors() {
        try {
            const totalRef = this.db.collection('analytics').doc('visitors');
            await totalRef.set({
                count: firebase.firestore.FieldValue.increment(1),
                lastUpdated: firebase.firestore.FieldValue.serverTimestamp()
            }, { merge: true });
        } catch (error) {
            console.error('Error updating total visitors:', error);
        }
    }

    // Track page-specific views
    async trackPageSpecificView(pagePath) {
        try {
            const pageRef = this.db.collection('analytics').doc('page_views').collection('pages').doc(pagePath);
            
            await pageRef.set({
                page: pagePath,
                count: firebase.firestore.FieldValue.increment(1),
                lastViewed: firebase.firestore.FieldValue.serverTimestamp()
            }, { merge: true });
        } catch (error) {
            console.error('Error tracking page-specific view:', error);
        }
    }

    // Add recent activity
    async addRecentActivity(description) {
        try {
            await this.db.collection('analytics').doc('recent_activity').collection('activities').add({
                description: description,
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                visitorId: this.visitorId,
                sessionId: this.sessionId,
                page: window.location.pathname
            });

            // Keep only last 100 activities
            const activitiesSnapshot = await this.db.collection('analytics')
                .doc('recent_activity')
                .collection('activities')
                .orderBy('timestamp', 'desc')
                .get();

            if (activitiesSnapshot.size > 100) {
                const toDelete = activitiesSnapshot.docs.slice(100);
                const batch = this.db.batch();
                toDelete.forEach(doc => batch.delete(doc.ref));
                await batch.commit();
            }
        } catch (error) {
            console.error('Error adding recent activity:', error);
        }
    }

    // Track custom events
    async trackEvent(eventName, eventData = {}) {
        if (!this.isInitialized) return;

        try {
            const event = {
                name: eventName,
                data: eventData,
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                sessionId: this.sessionId,
                visitorId: this.visitorId,
                page: window.location.pathname
            };

            await this.db.collection('analytics').doc('events').collection('custom').add(event);
            
            // Add to recent activity
            await this.addRecentActivity(`Event: ${eventName}`);

            // Track AR-specific events
            if (eventName.includes('ar_') || eventName.includes('navigation')) {
                await this.trackAREvent(eventName, eventData);
            }

            console.log('Event tracked:', eventName, eventData);
        } catch (error) {
            console.error('Error tracking event:', error);
        }
    }

    // Track AR-specific events
    async trackAREvent(eventName, eventData) {
        try {
            const today = new Date().toISOString().split('T')[0];
            
            // Track AR session start
            if (eventName === 'ar_session_start') {
                await this.db.collection('analytics').doc('ar_sessions').collection('active').doc(this.sessionId).set({
                    sessionId: this.sessionId,
                    visitorId: this.visitorId,
                    startTime: firebase.firestore.FieldValue.serverTimestamp(),
                    lastActivity: firebase.firestore.FieldValue.serverTimestamp(),
                    destination: eventData.destination || 'Unknown'
                });

                // Update daily AR sessions count
                await this.db.collection('analytics').doc('ar_sessions').collection('daily').doc(today).set({
                    count: firebase.firestore.FieldValue.increment(1),
                    date: today,
                    lastUpdated: firebase.firestore.FieldValue.serverTimestamp()
                }, { merge: true });
            }

            // Track destination visits
            if (eventName === 'destination_reached') {
                const destination = eventData.destination || 'Unknown';
                await this.db.collection('analytics').doc('destinations').collection('popular').doc(destination).set({
                    destination: destination,
                    visitCount: firebase.firestore.FieldValue.increment(1),
                    lastVisited: firebase.firestore.FieldValue.serverTimestamp()
                }, { merge: true });
            }

            // Track navigation success/failure
            if (eventName === 'navigation_completed') {
                const isSuccess = eventData.success || false;
                await this.updateSuccessRate(isSuccess);
            }

            // Update last activity for active sessions
            if (eventName.includes('ar_')) {
                await this.db.collection('analytics').doc('ar_sessions').collection('active').doc(this.sessionId).update({
                    lastActivity: firebase.firestore.FieldValue.serverTimestamp()
                });
            }

        } catch (error) {
            console.error('Error tracking AR event:', error);
        }
    }

    // Update success rate
    async updateSuccessRate(isSuccess) {
        try {
            const successRef = this.db.collection('analytics').doc('success_rate');
            const successDoc = await successRef.get();
            
            if (successDoc.exists) {
                const data = successDoc.data();
                const total = data.total || 0;
                const successful = data.successful || 0;
                
                const newTotal = total + 1;
                const newSuccessful = successful + (isSuccess ? 1 : 0);
                const newRate = (newSuccessful / newTotal) * 100;
                
                await successRef.update({
                    total: newTotal,
                    successful: newSuccessful,
                    rate: newRate,
                    lastUpdated: firebase.firestore.FieldValue.serverTimestamp()
                });
            } else {
                await successRef.set({
                    total: 1,
                    successful: isSuccess ? 1 : 0,
                    rate: isSuccess ? 100 : 0,
                    lastUpdated: firebase.firestore.FieldValue.serverTimestamp()
                });
            }
        } catch (error) {
            console.error('Error updating success rate:', error);
        }
    }

    // Setup event listeners
    setupEventListeners() {
        // Track clicks on important elements
        document.addEventListener('click', (e) => {
            const target = e.target;
            
            // Track button clicks
            if (target.tagName === 'BUTTON' || target.classList.contains('btn')) {
                this.trackEvent('button_click', {
                    buttonText: target.textContent.trim(),
                    buttonClass: target.className,
                    buttonId: target.id
                });
            }
            
            // Track link clicks
            if (target.tagName === 'A') {
                this.trackEvent('link_click', {
                    linkText: target.textContent.trim(),
                    linkHref: target.href,
                    linkTarget: target.target
                });
            }
        });

        // Track form submissions
        document.addEventListener('submit', (e) => {
            this.trackEvent('form_submit', {
                formId: e.target.id,
                formClass: e.target.className,
                formAction: e.target.action
            });
        });

        // Track scroll depth
        let maxScrollDepth = 0;
        window.addEventListener('scroll', () => {
            const scrollDepth = Math.round((window.scrollY / (document.body.scrollHeight - window.innerHeight)) * 100);
            if (scrollDepth > maxScrollDepth) {
                maxScrollDepth = scrollDepth;
                this.trackEvent('scroll_depth', {
                    depth: scrollDepth
                });
            }
        });

        // Track time on page
        setInterval(() => {
            const timeOnPage = Math.round((Date.now() - this.startTime) / 1000);
            if (timeOnPage % 30 === 0) { // Track every 30 seconds
                this.trackEvent('time_on_page', {
                    seconds: timeOnPage
                });
            }
        }, 1000);
    }

    // Get analytics data
    async getAnalyticsData(period = '30') {
        if (!this.isInitialized) return null;

        try {
            const days = parseInt(period);
            const endDate = new Date();
            const startDate = new Date();
            startDate.setDate(startDate.getDate() - days);

            const data = {
                totalVisitors: 0,
                todayVisitors: 0,
                uniqueVisitors: 0,
                pageViews: 0,
                dailyData: [],
                topPages: [],
                recentActivity: []
            };

            // Get total visitors
            const totalSnapshot = await this.db.collection('analytics').doc('visitors').get();
            data.totalVisitors = totalSnapshot.exists ? totalSnapshot.data().count || 0 : 0;

            // Get today's visitors
            const today = new Date().toISOString().split('T')[0];
            const todaySnapshot = await this.db.collection('analytics').doc('daily_visitors').collection('days').doc(today).get();
            data.todayVisitors = todaySnapshot.exists ? todaySnapshot.data().count || 0 : 0;

            // Get unique visitors
            const uniqueSnapshot = await this.db.collection('analytics')
                .doc('unique_visitors')
                .collection('visitors')
                .where('lastVisit', '>=', startDate)
                .get();
            data.uniqueVisitors = uniqueSnapshot.size;

            // Get page views
            const pageViewsSnapshot = await this.db.collection('analytics').doc('page_views').get();
            data.pageViews = pageViewsSnapshot.exists ? pageViewsSnapshot.data().count || 0 : 0;

            // Get daily data
            for (let i = days - 1; i >= 0; i--) {
                const date = new Date();
                date.setDate(date.getDate() - i);
                const dateStr = date.toISOString().split('T')[0];
                
                const daySnapshot = await this.db.collection('analytics')
                    .doc('daily_visitors')
                    .collection('days')
                    .doc(dateStr)
                    .get();
                
                data.dailyData.push({
                    date: dateStr,
                    visitors: daySnapshot.exists ? daySnapshot.data().count || 0 : 0
                });
            }

            // Get top pages
            const pagesSnapshot = await this.db.collection('analytics')
                .doc('page_views')
                .collection('pages')
                .orderBy('count', 'desc')
                .limit(10)
                .get();
            
            pagesSnapshot.forEach(doc => {
                data.topPages.push({
                    page: doc.id,
                    views: doc.data().count || 0
                });
            });

            // Get recent activity
            const activitySnapshot = await this.db.collection('analytics')
                .doc('recent_activity')
                .collection('activities')
                .orderBy('timestamp', 'desc')
                .limit(20)
                .get();
            
            activitySnapshot.forEach(doc => {
                data.recentActivity.push({
                    id: doc.id,
                    ...doc.data()
                });
            });

            return data;
        } catch (error) {
            console.error('Error getting analytics data:', error);
            return null;
        }
    }
}

// Initialize analytics when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    // Only initialize if not in admin analytics page (to avoid double tracking)
    if (!window.location.pathname.includes('analytics.html')) {
        window.analytics = new AnalyticsTracker();
    }
});

    // Helper functions for AR navigation tracking
    async trackARSessionStart(destination = 'Unknown') {
        await this.trackEvent('ar_session_start', { destination });
    }

    async trackDestinationReached(destination) {
        await this.trackEvent('destination_reached', { destination });
    }

    async trackNavigationCompleted(success, destination = 'Unknown', duration = 0) {
        await this.trackEvent('navigation_completed', { 
            success, 
            destination, 
            duration 
        });
    }

    async trackARError(errorType, errorMessage) {
        await this.trackEvent('ar_error', { 
            errorType, 
            errorMessage 
        });
    }

    async trackCameraPermission(granted) {
        await this.trackEvent('camera_permission', { granted });
    }

    async trackLocationPermission(granted) {
        await this.trackEvent('location_permission', { granted });
    }

    async trackCampusSelected(campus) {
        await this.trackEvent('campus_selected', { campus });
    }

    async trackTOSAccepted() {
        await this.trackEvent('tos_accepted');
    }
}

// Global helper functions for easy access
window.trackARSessionStart = function(destination) {
    if (window.analytics) {
        window.analytics.trackARSessionStart(destination);
    }
};

window.trackDestinationReached = function(destination) {
    if (window.analytics) {
        window.analytics.trackDestinationReached(destination);
    }
};

window.trackNavigationCompleted = function(success, destination, duration) {
    if (window.analytics) {
        window.analytics.trackNavigationCompleted(success, destination, duration);
    }
};

window.trackARError = function(errorType, errorMessage) {
    if (window.analytics) {
        window.analytics.trackARError(errorType, errorMessage);
    }
};

window.trackCameraPermission = function(granted) {
    if (window.analytics) {
        window.analytics.trackCameraPermission(granted);
    }
};

window.trackLocationPermission = function(granted) {
    if (window.analytics) {
        window.analytics.trackLocationPermission(granted);
    }
};

window.trackCampusSelected = function(campus) {
    if (window.analytics) {
        window.analytics.trackCampusSelected(campus);
    }
};

window.trackTOSAccepted = function() {
    if (window.analytics) {
        window.analytics.trackTOSAccepted();
    }
};

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AnalyticsTracker;
}
