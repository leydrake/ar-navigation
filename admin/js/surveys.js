// User Satisfaction Survey System
class SurveyCollector {
    constructor() {
        this.db = null;
        this.isInitialized = false;
        this.currentModal = null;
        
        this.init();
    }

    // Initialize Firebase and setup
    async init() {
        try {
            // Initialize Firebase if not already done
            if (typeof firebase === 'undefined') {
                console.warn('Firebase not loaded. Survey system will not work.');
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

            // Setup survey button
            this.setupSurveyButton();

        } catch (error) {
            console.error('Error initializing survey system:', error);
        }
    }

    // Setup survey button on pages
    setupSurveyButton() {
        // Create survey button if it doesn't exist
        if (!document.getElementById('surveyButton')) {
            this.createSurveyButton();
        }
    }

    // Create survey button
    createSurveyButton() {
        const surveyButton = document.createElement('button');
        surveyButton.id = 'surveyButton';
        surveyButton.className = 'survey-button';
        surveyButton.innerHTML = `
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
                <path d="M9 12l2 2 4-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
            </svg>
            <span>Survey</span>
        `;
        surveyButton.onclick = () => this.openSurveyModal();
        
        // Add to page
        document.body.appendChild(surveyButton);
    }

    // Open survey modal
    openSurveyModal() {
        if (this.currentModal) {
            this.closeSurveyModal();
        }

        this.currentModal = document.createElement('div');
        this.currentModal.className = 'survey-modal';
        this.currentModal.innerHTML = `
            <div class="survey-modal-content">
                <div class="survey-modal-header">
                    <h3>User Satisfaction Survey</h3>
                    <button class="survey-close-btn" onclick="window.surveyCollector.closeSurveyModal()">&times;</button>
                </div>
                <div class="survey-modal-body">
                    <form id="surveyForm">
                        <div class="form-group">
                            <label for="surveyType">Survey Type *</label>
                            <select id="surveyType" required>
                                <option value="">Select type...</option>
                                <option value="satisfaction">Overall Satisfaction</option>
                                <option value="experience">User Experience</option>
                                <option value="feature">Feature Request</option>
                            </select>
                        </div>
                        
                        <div class="form-group">
                            <label for="surveyTitle">Survey Title</label>
                            <input type="text" id="surveyTitle" placeholder="Brief title for this survey">
                        </div>
                        
                        <div class="rating-section">
                            <h4>Please rate your experience:</h4>
                            
                            <div class="rating-group">
                                <label for="overallRating">Overall Satisfaction *</label>
                                <div class="star-rating">
                                    <input type="radio" id="overall1" name="overallRating" value="1">
                                    <label for="overall1">★</label>
                                    <input type="radio" id="overall2" name="overallRating" value="2">
                                    <label for="overall2">★</label>
                                    <input type="radio" id="overall3" name="overallRating" value="3">
                                    <label for="overall3">★</label>
                                    <input type="radio" id="overall4" name="overallRating" value="4">
                                    <label for="overall4">★</label>
                                    <input type="radio" id="overall5" name="overallRating" value="5">
                                    <label for="overall5">★</label>
                                </div>
                            </div>
                            
                            <div class="rating-group">
                                <label for="easeOfUse">Ease of Use</label>
                                <div class="star-rating">
                                    <input type="radio" id="ease1" name="easeOfUse" value="1">
                                    <label for="ease1">★</label>
                                    <input type="radio" id="ease2" name="easeOfUse" value="2">
                                    <label for="ease2">★</label>
                                    <input type="radio" id="ease3" name="easeOfUse" value="3">
                                    <label for="ease3">★</label>
                                    <input type="radio" id="ease4" name="easeOfUse" value="4">
                                    <label for="ease4">★</label>
                                    <input type="radio" id="ease5" name="easeOfUse" value="5">
                                    <label for="ease5">★</label>
                                </div>
                            </div>
                            
                            <div class="rating-group">
                                <label for="designRating">Design & Interface</label>
                                <div class="star-rating">
                                    <input type="radio" id="design1" name="designRating" value="1">
                                    <label for="design1">★</label>
                                    <input type="radio" id="design2" name="designRating" value="2">
                                    <label for="design2">★</label>
                                    <input type="radio" id="design3" name="designRating" value="3">
                                    <label for="design3">★</label>
                                    <input type="radio" id="design4" name="designRating" value="4">
                                    <label for="design4">★</label>
                                    <input type="radio" id="design5" name="designRating" value="5">
                                    <label for="design5">★</label>
                                </div>
                            </div>
                            
                            <div class="rating-group">
                                <label for="performanceRating">Performance & Speed</label>
                                <div class="star-rating">
                                    <input type="radio" id="perf1" name="performanceRating" value="1">
                                    <label for="perf1">★</label>
                                    <input type="radio" id="perf2" name="performanceRating" value="2">
                                    <label for="perf2">★</label>
                                    <input type="radio" id="perf3" name="performanceRating" value="3">
                                    <label for="perf3">★</label>
                                    <input type="radio" id="perf4" name="performanceRating" value="4">
                                    <label for="perf4">★</label>
                                    <input type="radio" id="perf5" name="performanceRating" value="5">
                                    <label for="perf5">★</label>
                                </div>
                            </div>
                        </div>
                        
                        <div class="form-group">
                            <label for="surveyComments">Additional Comments</label>
                            <textarea id="surveyComments" rows="4" placeholder="Please share any additional feedback, suggestions, or comments..."></textarea>
                        </div>
                        
                        <div class="form-group">
                            <label for="surveyRecommendation">Would you recommend this system to others?</label>
                            <div class="radio-group">
                                <label class="radio-label">
                                    <input type="radio" name="recommendation" value="true">
                                    <span>Yes</span>
                                </label>
                                <label class="radio-label">
                                    <input type="radio" name="recommendation" value="false">
                                    <span>No</span>
                                </label>
                            </div>
                        </div>
                        
                        <div class="form-group">
                            <label for="surveyEmail">Email (optional)</label>
                            <input type="email" id="surveyEmail" placeholder="your.email@example.com">
                        </div>
                        
                        <div class="form-actions">
                            <button type="button" class="btn btn-secondary" onclick="window.surveyCollector.closeSurveyModal()">Cancel</button>
                            <button type="submit" class="btn btn-primary">Submit Survey</button>
                        </div>
                    </form>
                </div>
            </div>
        `;

        document.body.appendChild(this.currentModal);
        
        // Setup form submission
        document.getElementById('surveyForm').onsubmit = (e) => {
            e.preventDefault();
            this.submitSurvey();
        };

        // Add modal styles if not already added
        this.addModalStyles();
    }

    // Close survey modal
    closeSurveyModal() {
        if (this.currentModal) {
            this.currentModal.remove();
            this.currentModal = null;
        }
    }

    // Submit survey
    async submitSurvey() {
        if (!this.isInitialized) {
            alert('Survey system not initialized. Please try again later.');
            return;
        }

        try {
            const form = document.getElementById('surveyForm');
            
            // Get form values
            const surveyData = {
                type: document.getElementById('surveyType').value,
                title: document.getElementById('surveyTitle').value,
                overallRating: parseInt(document.querySelector('input[name="overallRating"]:checked')?.value) || null,
                easeOfUse: parseInt(document.querySelector('input[name="easeOfUse"]:checked')?.value) || null,
                designRating: parseInt(document.querySelector('input[name="designRating"]:checked')?.value) || null,
                performanceRating: parseInt(document.querySelector('input[name="performanceRating"]:checked')?.value) || null,
                comments: document.getElementById('surveyComments').value,
                recommendation: document.querySelector('input[name="recommendation"]:checked')?.value === 'true',
                email: document.getElementById('surveyEmail').value,
                page: window.location.pathname,
                userAgent: navigator.userAgent,
                timestamp: firebase.firestore.FieldValue.serverTimestamp()
            };

            // Validate required fields
            if (!surveyData.type || !surveyData.overallRating) {
                alert('Please fill in all required fields.');
                return;
            }

            // Show loading state
            const submitBtn = form.querySelector('button[type="submit"]');
            const originalText = submitBtn.textContent;
            submitBtn.textContent = 'Submitting...';
            submitBtn.disabled = true;

            // Submit to Firebase
            await this.db.collection('surveys').add(surveyData);

            // Track survey event
            await this.trackSurveyEvent('survey_submitted', surveyData);

            // Show success message
            alert('Thank you for your feedback! Your survey has been submitted successfully.');
            
            // Close modal
            this.closeSurveyModal();

        } catch (error) {
            console.error('Error submitting survey:', error);
            alert('Error submitting survey. Please try again later.');
        }
    }

    // Add modal styles
    addModalStyles() {
        if (document.getElementById('surveyModalStyles')) return;

        const style = document.createElement('style');
        style.id = 'surveyModalStyles';
        style.textContent = `
            .survey-button {
                position: fixed;
                bottom: 140px;
                right: 20px;
                background: #6f42c1;
                color: white;
                border: none;
                border-radius: 50px;
                padding: 12px 20px;
                font-size: 14px;
                font-weight: 500;
                cursor: pointer;
                box-shadow: 0 4px 12px rgba(111, 66, 193, 0.3);
                display: flex;
                align-items: center;
                gap: 8px;
                z-index: 1000;
                transition: all 0.3s ease;
            }
            
            .survey-button:hover {
                background: #5a2d91;
                transform: translateY(-2px);
                box-shadow: 0 6px 16px rgba(111, 66, 193, 0.4);
            }
            
            .survey-modal {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.5);
                z-index: 2000;
                display: flex;
                align-items: center;
                justify-content: center;
                padding: 20px;
            }
            
            .survey-modal-content {
                background: white;
                border-radius: 12px;
                width: 100%;
                max-width: 600px;
                max-height: 90vh;
                overflow: hidden;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
            }
            
            .survey-modal-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 20px;
                border-bottom: 1px solid #e9ecef;
                background: #f8f9fa;
            }
            
            .survey-modal-header h3 {
                margin: 0;
                font-size: 1.25rem;
                font-weight: 600;
                color: #2c3e50;
            }
            
            .survey-close-btn {
                background: none;
                border: none;
                font-size: 1.5rem;
                color: #6c757d;
                cursor: pointer;
                padding: 4px;
                line-height: 1;
            }
            
            .survey-close-btn:hover {
                color: #2c3e50;
            }
            
            .survey-modal-body {
                padding: 20px;
                max-height: 70vh;
                overflow-y: auto;
            }
            
            .form-group {
                margin-bottom: 20px;
            }
            
            .form-group label {
                display: block;
                margin-bottom: 6px;
                font-weight: 500;
                color: #2c3e50;
                font-size: 14px;
            }
            
            .form-group input,
            .form-group select,
            .form-group textarea {
                width: 100%;
                padding: 10px 12px;
                border: 1px solid #dee2e6;
                border-radius: 6px;
                font-size: 14px;
                color: #2c3e50;
                background: white;
                transition: border-color 0.2s ease;
            }
            
            .form-group input:focus,
            .form-group select:focus,
            .form-group textarea:focus {
                outline: none;
                border-color: #6f42c1;
                box-shadow: 0 0 0 2px rgba(111, 66, 193, 0.1);
            }
            
            .form-group textarea {
                resize: vertical;
                min-height: 80px;
            }
            
            .rating-section {
                background: #f8f9fa;
                padding: 20px;
                border-radius: 8px;
                margin-bottom: 20px;
            }
            
            .rating-section h4 {
                margin: 0 0 20px 0;
                color: #2c3e50;
                font-size: 1.1rem;
            }
            
            .rating-group {
                margin-bottom: 20px;
            }
            
            .rating-group label {
                display: block;
                margin-bottom: 8px;
                font-weight: 500;
                color: #2c3e50;
            }
            
            .star-rating {
                display: flex;
                gap: 4px;
                align-items: center;
            }
            
            .star-rating input[type="radio"] {
                display: none;
            }
            
            .star-rating label {
                font-size: 24px;
                cursor: pointer;
                color: #ddd;
                transition: color 0.2s ease;
                margin: 0;
                padding: 4px;
            }
            
            .star-rating input[type="radio"]:checked ~ label,
            .star-rating label:hover {
                color: #f9a825;
            }
            
            .radio-group {
                display: flex;
                gap: 20px;
            }
            
            .radio-label {
                display: flex;
                align-items: center;
                gap: 8px;
                cursor: pointer;
                font-weight: normal;
            }
            
            .radio-label input[type="radio"] {
                width: auto;
                margin: 0;
            }
            
            .form-actions {
                display: flex;
                gap: 12px;
                justify-content: flex-end;
                margin-top: 30px;
                padding-top: 20px;
                border-top: 1px solid #e9ecef;
            }
            
            .btn {
                padding: 10px 20px;
                border: none;
                border-radius: 6px;
                font-size: 14px;
                font-weight: 500;
                cursor: pointer;
                transition: all 0.2s ease;
            }
            
            .btn-secondary {
                background: #f8f9fa;
                color: #6c757d;
                border: 1px solid #dee2e6;
            }
            
            .btn-secondary:hover {
                background: #e9ecef;
                color: #495057;
            }
            
            .btn-primary {
                background: #6f42c1;
                color: white;
            }
            
            .btn-primary:hover {
                background: #5a2d91;
            }
            
            .btn-primary:disabled {
                background: #6c757d;
                cursor: not-allowed;
            }
            
            @media (max-width: 480px) {
                .survey-modal {
                    padding: 10px;
                }
                
                .survey-modal-content {
                    max-height: 95vh;
                }
                
                .radio-group {
                    flex-direction: column;
                    gap: 10px;
                }
                
                .form-actions {
                    flex-direction: column;
                }
                
                .btn {
                    width: 100%;
                }
            }
        `;
        
        document.head.appendChild(style);
    }

    // Track survey events for analytics
    async trackSurveyEvent(eventType, surveyData = {}) {
        if (!this.isInitialized) return;

        try {
            await this.db.collection('analytics').doc('events').collection('surveys').add({
                eventType: eventType,
                surveyType: surveyData.type,
                overallRating: surveyData.overallRating,
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                page: window.location.pathname,
                userAgent: navigator.userAgent
            });
        } catch (error) {
            console.error('Error tracking survey event:', error);
        }
    }

    // Helper functions for easy access
    async submitQuickSurvey(rating, comments = '', type = 'satisfaction') {
        if (!this.isInitialized) {
            console.warn('Survey system not initialized');
            return;
        }

        try {
            const surveyData = {
                type: type,
                title: 'Quick Survey',
                overallRating: rating,
                comments: comments,
                page: window.location.pathname,
                userAgent: navigator.userAgent,
                timestamp: firebase.firestore.FieldValue.serverTimestamp()
            };

            await this.db.collection('surveys').add(surveyData);
            await this.trackSurveyEvent('quick_survey_submitted', surveyData);
            
            console.log('Quick survey submitted successfully');
        } catch (error) {
            console.error('Error submitting quick survey:', error);
        }
    }
}

// Initialize survey collector when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    // Only initialize if not in admin surveys page
    if (!window.location.pathname.includes('surveys.html')) {
        window.surveyCollector = new SurveyCollector();
    }
});

// Global functions for easy access
window.openSurveyModal = function() {
    if (window.surveyCollector) {
        window.surveyCollector.openSurveyModal();
    }
};

window.closeSurveyModal = function() {
    if (window.surveyCollector) {
        window.surveyCollector.closeSurveyModal();
    }
};

window.submitQuickSurvey = function(rating, comments, type) {
    if (window.surveyCollector) {
        window.surveyCollector.submitQuickSurvey(rating, comments, type);
    }
};

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SurveyCollector;
}
