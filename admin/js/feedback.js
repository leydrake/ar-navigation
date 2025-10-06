// Feedback Collection System
class FeedbackCollector {
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
                console.warn('Firebase not loaded. Feedback system will not work.');
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

            // Setup feedback button
            this.setupFeedbackButton();

        } catch (error) {
            console.error('Error initializing feedback system:', error);
        }
    }

    // Setup feedback button on pages
    setupFeedbackButton() {
        // Create feedback button if it doesn't exist
        if (!document.getElementById('feedbackButton')) {
            this.createFeedbackButton();
        }
    }

    // Create feedback button
    createFeedbackButton() {
        const feedbackButton = document.createElement('button');
        feedbackButton.id = 'feedbackButton';
        feedbackButton.className = 'feedback-button';
        feedbackButton.innerHTML = `
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
                <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span>Feedback</span>
        `;
        feedbackButton.onclick = () => this.openFeedbackModal();
        
        // Add to page
        document.body.appendChild(feedbackButton);
    }

    // Open feedback modal
    openFeedbackModal() {
        if (this.currentModal) {
            this.closeFeedbackModal();
        }

        this.currentModal = document.createElement('div');
        this.currentModal.className = 'feedback-modal';
        this.currentModal.innerHTML = `
            <div class="feedback-modal-content">
                <div class="feedback-modal-header">
                    <h3>Send Feedback</h3>
                    <button class="feedback-close-btn" onclick="window.feedbackCollector.closeFeedbackModal()">&times;</button>
                </div>
                <div class="feedback-modal-body">
                    <form id="feedbackForm">
                        <div class="form-group">
                            <label for="feedbackType">Type of Feedback *</label>
                            <select id="feedbackType" required>
                                <option value="">Select type...</option>
                                <option value="bug">Bug Report</option>
                                <option value="feature">Feature Request</option>
                                <option value="general">General Feedback</option>
                                <option value="support">Support Request</option>
                            </select>
                        </div>
                        
                        <div class="form-group">
                            <label for="feedbackPriority">Priority</label>
                            <select id="feedbackPriority">
                                <option value="medium">Medium</option>
                                <option value="low">Low</option>
                                <option value="high">High</option>
                            </select>
                        </div>
                        
                        <div class="form-group">
                            <label for="feedbackTitle">Title *</label>
                            <input type="text" id="feedbackTitle" placeholder="Brief description of your feedback" required>
                        </div>
                        
                        <div class="form-group">
                            <label for="feedbackDescription">Description *</label>
                            <textarea id="feedbackDescription" rows="4" placeholder="Please provide detailed information about your feedback..." required></textarea>
                        </div>
                        
                        <div class="form-group">
                            <label for="feedbackEmail">Email (optional)</label>
                            <input type="email" id="feedbackEmail" placeholder="your.email@example.com">
                        </div>
                        
                        <div class="form-group">
                            <label for="feedbackRating">Rating (optional)</label>
                            <div class="rating-input">
                                <input type="radio" id="rating1" name="rating" value="1">
                                <label for="rating1">⭐</label>
                                <input type="radio" id="rating2" name="rating" value="2">
                                <label for="rating2">⭐</label>
                                <input type="radio" id="rating3" name="rating" value="3">
                                <label for="rating3">⭐</label>
                                <input type="radio" id="rating4" name="rating" value="4">
                                <label for="rating4">⭐</label>
                                <input type="radio" id="rating5" name="rating" value="5">
                                <label for="rating5">⭐</label>
                            </div>
                        </div>
                        
                        <div class="form-group">
                            <label for="feedbackAttachments">Attachments (optional)</label>
                            <input type="file" id="feedbackAttachments" multiple accept="image/*,.pdf,.doc,.docx">
                        </div>
                        
                        <div class="form-actions">
                            <button type="button" class="btn btn-secondary" onclick="window.feedbackCollector.closeFeedbackModal()">Cancel</button>
                            <button type="submit" class="btn btn-primary">Submit Feedback</button>
                        </div>
                    </form>
                </div>
            </div>
        `;

        document.body.appendChild(this.currentModal);
        
        // Setup form submission
        document.getElementById('feedbackForm').onsubmit = (e) => {
            e.preventDefault();
            this.submitFeedback();
        };

        // Add modal styles if not already added
        this.addModalStyles();
    }

    // Close feedback modal
    closeFeedbackModal() {
        if (this.currentModal) {
            this.currentModal.remove();
            this.currentModal = null;
        }
    }

    // Submit feedback
    async submitFeedback() {
        if (!this.isInitialized) {
            alert('Feedback system not initialized. Please try again later.');
            return;
        }

        try {
            const form = document.getElementById('feedbackForm');
            const formData = new FormData(form);
            
            // Get form values
            const feedbackData = {
                type: document.getElementById('feedbackType').value,
                priority: document.getElementById('feedbackPriority').value,
                title: document.getElementById('feedbackTitle').value,
                description: document.getElementById('feedbackDescription').value,
                email: document.getElementById('feedbackEmail').value,
                rating: document.querySelector('input[name="rating"]:checked')?.value || null,
                page: window.location.pathname,
                userAgent: navigator.userAgent,
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                status: 'new',
                attachments: []
            };

            // Handle file attachments
            const fileInput = document.getElementById('feedbackAttachments');
            if (fileInput.files.length > 0) {
                // For now, just store file names - in production, upload to storage
                feedbackData.attachments = Array.from(fileInput.files).map(file => ({
                    name: file.name,
                    size: file.size,
                    type: file.type
                }));
            }

            // Validate required fields using centralized validation
            const form = document.getElementById('feedbackForm');
            const validationRules = {
                feedbackType: { required: true },
                feedbackTitle: { required: true, minLength: 5, maxLength: 200 },
                feedbackDescription: { required: true, minLength: 10, maxLength: 1000 },
                feedbackEmail: { email: true },
                feedbackPriority: { required: true }
            };
            
            const validation = window.validationUtils.validateForm(form, validationRules);
            if (!validation.isValid) {
                window.validationUtils.showFormErrors(form, validation.errors);
                return;
            }

            // Show loading state
            const submitBtn = form.querySelector('button[type="submit"]');
            const originalText = submitBtn.textContent;
            submitBtn.textContent = 'Submitting...';
            submitBtn.disabled = true;

            // Submit to Firebase
            await this.db.collection('feedback').add(feedbackData);

            // Show success message
            alert('Thank you for your feedback! We will review it and get back to you if needed.');
            
            // Close modal
            this.closeFeedbackModal();

        } catch (error) {
            console.error('Error submitting feedback:', error);
            alert('Error submitting feedback. Please try again later.');
        }
    }

    // Add modal styles
    addModalStyles() {
        if (document.getElementById('feedbackModalStyles')) return;

        const style = document.createElement('style');
        style.id = 'feedbackModalStyles';
        style.textContent = `
            .feedback-button {
                position: fixed;
                bottom: 20px;
                right: 20px;
                background: #206233;
                color: white;
                border: none;
                border-radius: 50px;
                padding: 12px 20px;
                font-size: 14px;
                font-weight: 500;
                cursor: pointer;
                box-shadow: 0 4px 12px rgba(32, 98, 51, 0.3);
                display: flex;
                align-items: center;
                gap: 8px;
                z-index: 1000;
                transition: all 0.3s ease;
            }
            
            .feedback-button:hover {
                background: #1a4d2e;
                transform: translateY(-2px);
                box-shadow: 0 6px 16px rgba(32, 98, 51, 0.4);
            }
            
            .feedback-modal {
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
            
            .feedback-modal-content {
                background: white;
                border-radius: 12px;
                width: 100%;
                max-width: 500px;
                max-height: 90vh;
                overflow: hidden;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
            }
            
            .feedback-modal-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 20px;
                border-bottom: 1px solid #e9ecef;
                background: #f8f9fa;
            }
            
            .feedback-modal-header h3 {
                margin: 0;
                font-size: 1.25rem;
                font-weight: 600;
                color: #2c3e50;
            }
            
            .feedback-close-btn {
                background: none;
                border: none;
                font-size: 1.5rem;
                color: #6c757d;
                cursor: pointer;
                padding: 4px;
                line-height: 1;
            }
            
            .feedback-close-btn:hover {
                color: #2c3e50;
            }
            
            .feedback-modal-body {
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
                border-color: #206233;
                box-shadow: 0 0 0 2px rgba(32, 98, 51, 0.1);
            }
            
            .form-group textarea {
                resize: vertical;
                min-height: 80px;
            }
            
            .rating-input {
                display: flex;
                gap: 4px;
                align-items: center;
            }
            
            .rating-input input[type="radio"] {
                display: none;
            }
            
            .rating-input label {
                font-size: 20px;
                cursor: pointer;
                color: #ddd;
                transition: color 0.2s ease;
                margin: 0;
            }
            
            .rating-input input[type="radio"]:checked ~ label,
            .rating-input label:hover {
                color: #ffc107;
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
                background: #206233;
                color: white;
            }
            
            .btn-primary:hover {
                background: #1a4d2e;
            }
            
            .btn-primary:disabled {
                background: #6c757d;
                cursor: not-allowed;
            }
            
            @media (max-width: 480px) {
                .feedback-modal {
                    padding: 10px;
                }
                
                .feedback-modal-content {
                    max-height: 95vh;
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

    // Track feedback events for analytics
    async trackFeedbackEvent(eventType, feedbackData = {}) {
        if (!this.isInitialized) return;

        try {
            await this.db.collection('analytics').doc('events').collection('feedback').add({
                eventType: eventType,
                feedbackType: feedbackData.type,
                timestamp: firebase.firestore.FieldValue.serverTimestamp(),
                page: window.location.pathname,
                userAgent: navigator.userAgent
            });
        } catch (error) {
            console.error('Error tracking feedback event:', error);
        }
    }
}

// Initialize feedback collector when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    // Only initialize if not in admin feedback page
    if (!window.location.pathname.includes('feedback.html')) {
        window.feedbackCollector = new FeedbackCollector();
    }
});

// Global functions for easy access
window.openFeedbackModal = function() {
    if (window.feedbackCollector) {
        window.feedbackCollector.openFeedbackModal();
    }
};

window.closeFeedbackModal = function() {
    if (window.feedbackCollector) {
        window.feedbackCollector.closeFeedbackModal();
    }
};

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FeedbackCollector;
}
