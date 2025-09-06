// Bug Reporting System
class BugReporter {
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
                console.warn('Firebase not loaded. Bug reporting system will not work.');
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

            // Setup bug reporting button
            this.setupBugReportingButton();

        } catch (error) {
            console.error('Error initializing bug reporting system:', error);
        }
    }

    // Setup bug reporting button on pages
    setupBugReportingButton() {
        // Create bug reporting button if it doesn't exist
        if (!document.getElementById('bugReportingButton')) {
            this.createBugReportingButton();
        }
    }

    // Create bug reporting button
    createBugReportingButton() {
        const bugButton = document.createElement('button');
        bugButton.id = 'bugReportingButton';
        bugButton.className = 'bug-reporting-button';
        bugButton.innerHTML = `
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span>Report Bug</span>
        `;
        bugButton.onclick = () => this.openBugReportingModal();
        
        // Add to page
        document.body.appendChild(bugButton);
    }

    // Open bug reporting modal
    openBugReportingModal() {
        if (this.currentModal) {
            this.closeBugReportingModal();
        }

        this.currentModal = document.createElement('div');
        this.currentModal.className = 'bug-reporting-modal';
        this.currentModal.innerHTML = `
            <div class="bug-reporting-modal-content">
                <div class="bug-reporting-modal-header">
                    <h3>Report a Bug</h3>
                    <button class="bug-reporting-close-btn" onclick="window.bugReporter.closeBugReportingModal()">&times;</button>
                </div>
                <div class="bug-reporting-modal-body">
                    <form id="bugReportingForm">
                        <div class="form-group">
                            <label for="bugTitle">Bug Title *</label>
                            <input type="text" id="bugTitle" placeholder="Brief description of the bug" required>
                        </div>
                        
                        <div class="form-row">
                            <div class="form-group">
                                <label for="bugCategory">Category *</label>
                                <select id="bugCategory" required>
                                    <option value="">Select category...</option>
                                    <option value="ar_navigation">AR Navigation</option>
                                    <option value="camera">Camera</option>
                                    <option value="location">Location</option>
                                    <option value="ui_ux">UI/UX</option>
                                    <option value="performance">Performance</option>
                                    <option value="crash">Crash</option>
                                    <option value="other">Other</option>
                                </select>
                            </div>
                            
                            <div class="form-group">
                                <label for="bugSeverity">Severity *</label>
                                <select id="bugSeverity" required>
                                    <option value="">Select severity...</option>
                                    <option value="critical">Critical - System unusable</option>
                                    <option value="high">High - Major functionality broken</option>
                                    <option value="medium">Medium - Minor functionality broken</option>
                                    <option value="low">Low - Cosmetic issue</option>
                                </select>
                            </div>
                        </div>
                        
                        <div class="form-group">
                            <label for="bugDescription">Bug Description *</label>
                            <textarea id="bugDescription" rows="4" placeholder="Describe what went wrong..." required></textarea>
                        </div>
                        
                        <div class="form-group">
                            <label for="bugSteps">Steps to Reproduce *</label>
                            <textarea id="bugSteps" rows="3" placeholder="1. Go to...&#10;2. Click on...&#10;3. See error..." required></textarea>
                        </div>
                        
                        <div class="form-group">
                            <label for="bugExpected">Expected Behavior</label>
                            <textarea id="bugExpected" rows="2" placeholder="What should have happened?"></textarea>
                        </div>
                        
                        <div class="form-group">
                            <label for="bugActual">Actual Behavior</label>
                            <textarea id="bugActual" rows="2" placeholder="What actually happened?"></textarea>
                        </div>
                        
                        <div class="form-group">
                            <label for="bugEmail">Email (optional)</label>
                            <input type="email" id="bugEmail" placeholder="your.email@example.com">
                        </div>
                        
                        <div class="form-group">
                            <label for="bugAttachments">Screenshots/Attachments</label>
                            <input type="file" id="bugAttachments" multiple accept="image/*,.pdf,.doc,.docx">
                            <small>Upload screenshots or files that help explain the bug</small>
                        </div>
                        
                        <div class="form-actions">
                            <button type="button" class="btn btn-secondary" onclick="window.bugReporter.closeBugReportingModal()">Cancel</button>
                            <button type="submit" class="btn btn-primary">Submit Bug Report</button>
                        </div>
                    </form>
                </div>
            </div>
        `;

        document.body.appendChild(this.currentModal);
        
        // Setup form submission
        document.getElementById('bugReportingForm').onsubmit = (e) => {
            e.preventDefault();
            this.submitBugReport();
        };

        // Add modal styles if not already added
        this.addModalStyles();
    }

    // Close bug reporting modal
    closeBugReportingModal() {
        if (this.currentModal) {
            this.currentModal.remove();
            this.currentModal = null;
        }
    }

    // Submit bug report
    async submitBugReport() {
        if (!this.isInitialized) {
            alert('Bug reporting system not initialized. Please try again later.');
            return;
        }

        try {
            const form = document.getElementById('bugReportingForm');
            
            // Get form values
            const bugData = {
                title: document.getElementById('bugTitle').value,
                category: document.getElementById('bugCategory').value,
                severity: document.getElementById('bugSeverity').value,
                description: document.getElementById('bugDescription').value,
                stepsToReproduce: document.getElementById('bugSteps').value,
                expectedBehavior: document.getElementById('bugExpected').value,
                actualBehavior: document.getElementById('bugActual').value,
                email: document.getElementById('bugEmail').value,
                page: window.location.pathname,
                userAgent: navigator.userAgent,
                browserInfo: this.getBrowserInfo(),
                screenResolution: `${screen.width}x${screen.height}`,
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                status: 'new',
                attachments: []
            };

            // Handle file attachments
            const fileInput = document.getElementById('bugAttachments');
            if (fileInput.files.length > 0) {
                // For now, just store file names - in production, upload to storage
                bugData.attachments = Array.from(fileInput.files).map(file => ({
                    name: file.name,
                    size: file.size,
                    type: file.type
                }));
            }

            // Validate required fields
            if (!bugData.title || !bugData.category || !bugData.severity || !bugData.description || !bugData.stepsToReproduce) {
                alert('Please fill in all required fields.');
                return;
            }

            // Show loading state
            const submitBtn = form.querySelector('button[type="submit"]');
            const originalText = submitBtn.textContent;
            submitBtn.textContent = 'Submitting...';
            submitBtn.disabled = true;

            // Submit to Firebase
            await this.db.collection('bug_reports').add(bugData);

            // Track bug report event
            await this.trackBugEvent('bug_report_submitted', bugData);

            // Show success message
            alert('Thank you for reporting this bug! We will investigate and fix it as soon as possible.');
            
            // Close modal
            this.closeBugReportingModal();

        } catch (error) {
            console.error('Error submitting bug report:', error);
            alert('Error submitting bug report. Please try again later.');
        }
    }

    // Get browser information
    getBrowserInfo() {
        const ua = navigator.userAgent;
        let browserName = 'Unknown';
        let browserVersion = 'Unknown';
        
        if (ua.includes('Chrome')) {
            browserName = 'Chrome';
            browserVersion = ua.match(/Chrome\/(\d+)/)?.[1] || 'Unknown';
        } else if (ua.includes('Firefox')) {
            browserName = 'Firefox';
            browserVersion = ua.match(/Firefox\/(\d+)/)?.[1] || 'Unknown';
        } else if (ua.includes('Safari')) {
            browserName = 'Safari';
            browserVersion = ua.match(/Version\/(\d+)/)?.[1] || 'Unknown';
        } else if (ua.includes('Edge')) {
            browserName = 'Edge';
            browserVersion = ua.match(/Edge\/(\d+)/)?.[1] || 'Unknown';
        }
        
        return `${browserName} ${browserVersion}`;
    }

    // Add modal styles
    addModalStyles() {
        if (document.getElementById('bugReportingModalStyles')) return;

        const style = document.createElement('style');
        style.id = 'bugReportingModalStyles';
        style.textContent = `
            .bug-reporting-button {
                position: fixed;
                bottom: 80px;
                right: 20px;
                background: #dc3545;
                color: white;
                border: none;
                border-radius: 50px;
                padding: 12px 20px;
                font-size: 14px;
                font-weight: 500;
                cursor: pointer;
                box-shadow: 0 4px 12px rgba(220, 53, 69, 0.3);
                display: flex;
                align-items: center;
                gap: 8px;
                z-index: 1000;
                transition: all 0.3s ease;
            }
            
            .bug-reporting-button:hover {
                background: #c82333;
                transform: translateY(-2px);
                box-shadow: 0 6px 16px rgba(220, 53, 69, 0.4);
            }
            
            .bug-reporting-modal {
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
            
            .bug-reporting-modal-content {
                background: white;
                border-radius: 12px;
                width: 100%;
                max-width: 600px;
                max-height: 90vh;
                overflow: hidden;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
            }
            
            .bug-reporting-modal-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 20px;
                border-bottom: 1px solid #e9ecef;
                background: #f8f9fa;
            }
            
            .bug-reporting-modal-header h3 {
                margin: 0;
                font-size: 1.25rem;
                font-weight: 600;
                color: #2c3e50;
            }
            
            .bug-reporting-close-btn {
                background: none;
                border: none;
                font-size: 1.5rem;
                color: #6c757d;
                cursor: pointer;
                padding: 4px;
                line-height: 1;
            }
            
            .bug-reporting-close-btn:hover {
                color: #2c3e50;
            }
            
            .bug-reporting-modal-body {
                padding: 20px;
                max-height: 70vh;
                overflow-y: auto;
            }
            
            .form-group {
                margin-bottom: 20px;
            }
            
            .form-row {
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 15px;
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
                border-color: #dc3545;
                box-shadow: 0 0 0 2px rgba(220, 53, 69, 0.1);
            }
            
            .form-group textarea {
                resize: vertical;
                min-height: 80px;
            }
            
            .form-group small {
                display: block;
                margin-top: 4px;
                font-size: 12px;
                color: #6c757d;
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
                background: #dc3545;
                color: white;
            }
            
            .btn-primary:hover {
                background: #c82333;
            }
            
            .btn-primary:disabled {
                background: #6c757d;
                cursor: not-allowed;
            }
            
            @media (max-width: 480px) {
                .bug-reporting-modal {
                    padding: 10px;
                }
                
                .bug-reporting-modal-content {
                    max-height: 95vh;
                }
                
                .form-row {
                    grid-template-columns: 1fr;
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

    // Track bug events for analytics
    async trackBugEvent(eventType, bugData = {}) {
        if (!this.isInitialized) return;

        try {
            await this.db.collection('analytics').doc('events').collection('bugs').add({
                eventType: eventType,
                bugCategory: bugData.category,
                bugSeverity: bugData.severity,
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                page: window.location.pathname,
                userAgent: navigator.userAgent
            });
        } catch (error) {
            console.error('Error tracking bug event:', error);
        }
    }

    // Helper functions for easy access
    async reportBug(title, description, category = 'other', severity = 'medium') {
        if (!this.isInitialized) {
            console.warn('Bug reporting system not initialized');
            return;
        }

        try {
            const bugData = {
                title: title,
                category: category,
                severity: severity,
                description: description,
                stepsToReproduce: 'Reported programmatically',
                page: window.location.pathname,
                userAgent: navigator.userAgent,
                browserInfo: this.getBrowserInfo(),
                screenResolution: `${screen.width}x${screen.height}`,
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                status: 'new'
            };

            await this.db.collection('bug_reports').add(bugData);
            await this.trackBugEvent('bug_report_submitted', bugData);
            
            console.log('Bug report submitted successfully');
        } catch (error) {
            console.error('Error submitting bug report:', error);
        }
    }
}

// Initialize bug reporter when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    // Only initialize if not in admin bug reporting page
    if (!window.location.pathname.includes('bug-reporting.html')) {
        window.bugReporter = new BugReporter();
    }
});

// Global functions for easy access
window.openBugReportingModal = function() {
    if (window.bugReporter) {
        window.bugReporter.openBugReportingModal();
    }
};

window.closeBugReportingModal = function() {
    if (window.bugReporter) {
        window.bugReporter.closeBugReportingModal();
    }
};

window.reportBug = function(title, description, category, severity) {
    if (window.bugReporter) {
        window.bugReporter.reportBug(title, description, category, severity);
    }
};

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = BugReporter;
}
