// Authentication and Session Management System
class AdminAuth {
    constructor() {
        this.sessionKey = 'adminSession';
        this.sessionTimeout = 8 * 60 * 60 * 1000; // 8 hours
        this.checkInterval = 5 * 60 * 1000; // Check every 5 minutes
        this.init();
    }

    init() {
        this.checkAuth();
        this.startSessionMonitoring();
        this.setupLogoutHandlers();
    }

    // Check if user is authenticated
    checkAuth() {
        const session = this.getSession();
        
        if (!session) {
            this.redirectToLogin('Please log in to access this page');
            return false;
        }

        // Check if session has expired
        if (this.isSessionExpired(session)) {
            this.clearSession();
            this.redirectToLogin('Your session has expired. Please log in again');
            return false;
        }

        // Check if user is on the correct page
        this.validatePageAccess();
        
        return true;
    }

    // Get current session
    getSession() {
        try {
            const sessionData = localStorage.getItem(this.sessionKey);
            return sessionData ? JSON.parse(sessionData) : null;
        } catch (error) {
            console.error('Error parsing session data:', error);
            return null;
        }
    }

    // Check if session is expired
    isSessionExpired(session) {
        if (!session.loginTime) return true;
        
        const loginTime = new Date(session.loginTime).getTime();
        const currentTime = new Date().getTime();
        
        return (currentTime - loginTime) > this.sessionTimeout;
    }

    // Validate page access based on user role
    validatePageAccess() {
        const session = this.getSession();
        const currentPage = window.location.pathname.split('/').pop();
        
        // Define page access rules
        const pageAccess = {
            'admin-tools.html': ['admin', 'super_admin'],
            'admin-settings.html': ['admin', 'super_admin'],
            'events.html': ['admin', 'super_admin', 'event_manager'],
            'add-location.html': ['admin', 'super_admin'],
            'edit-location.html': ['admin', 'super_admin'],
            'change-destination.html': ['admin', 'super_admin']
        };

        if (pageAccess[currentPage] && !pageAccess[currentPage].includes(session.role)) {
            this.showError('Access denied. You do not have permission to view this page.');
            setTimeout(() => {
                window.location.href = 'admin-tools.html';
            }, 2000);
        }
    }

    // Start session monitoring
    startSessionMonitoring() {
        setInterval(() => {
            const session = this.getSession();
            if (session && this.isSessionExpired(session)) {
                this.clearSession();
                this.redirectToLogin('Your session has expired. Please log in again');
            }
        }, this.checkInterval);
    }

    // Setup logout handlers
    setupLogoutHandlers() {
        // Add logout button to header if it doesn't exist
        this.addLogoutButton();
        
        // Handle logout clicks
        document.addEventListener('click', (e) => {
            if (e.target.matches('.logout-btn') || e.target.closest('.logout-btn')) {
                this.logout();
            }
        });
    }

    // Add logout button to header
    addLogoutButton() {
        const headers = document.querySelectorAll('.dashboard-header, .settings-header, .events-header, .add-location-header');
        
        headers.forEach(header => {
            if (!header.querySelector('.logout-btn')) {
                const headerRight = header.querySelector('.header-right') || header;
                
                const logoutBtn = document.createElement('button');
                logoutBtn.className = 'logout-btn';
                logoutBtn.title = 'Logout';
                logoutBtn.innerHTML = `
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                        <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        <polyline points="16,17 21,12 16,7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        <line x1="21" y1="12" x2="9" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                `;
                
                headerRight.appendChild(logoutBtn);
            }
        });
    }

    // Logout function
    logout() {
        const confirmed = confirm('Are you sure you want to logout?');
        if (confirmed) {
            this.clearSession();
            this.redirectToLogin('You have been logged out successfully');
        }
    }

    // Clear session data
    clearSession() {
        localStorage.removeItem(this.sessionKey);
        sessionStorage.clear();
    }

    // Redirect to login page
    redirectToLogin(message = '') {
        if (message) {
            sessionStorage.setItem('loginMessage', message);
        }
        window.location.href = 'index.html';
    }

    // Show error message
    showError(message) {
        // Create error notification
        const notification = document.createElement('div');
        notification.className = 'auth-error-notification';
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-message">${message}</span>
                <button class="notification-close">&times;</button>
            </div>
        `;
        
        // Add styles
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: #ff4444;
            color: white;
            padding: 15px 20px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 10000;
            max-width: 400px;
            animation: slideIn 0.3s ease-out;
        `;
        
        // Add close button functionality
        notification.querySelector('.notification-close').onclick = () => {
            notification.remove();
        };
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
        
        document.body.appendChild(notification);
    }

    // Get user info
    getUserInfo() {
        const session = this.getSession();
        return session ? {
            email: session.email,
            role: session.role,
            loginTime: session.loginTime
        } : null;
    }

    // Check if user has specific permission
    hasPermission(permission) {
        const session = this.getSession();
        if (!session) return false;
        
        const permissions = {
            'admin': ['read', 'write', 'delete', 'manage_events', 'manage_locations'],
            'super_admin': ['read', 'write', 'delete', 'manage_events', 'manage_locations', 'manage_users', 'system_settings'],
            'event_manager': ['read', 'write', 'manage_events']
        };
        
        return permissions[session.role]?.includes(permission) || false;
    }

    // Refresh session (extend timeout)
    refreshSession() {
        const session = this.getSession();
        if (session) {
            session.loginTime = new Date().toISOString();
            localStorage.setItem(this.sessionKey, JSON.stringify(session));
        }
    }
}

// Initialize authentication when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Skip auth check on login page
    if (!window.location.pathname.includes('index.html')) {
        window.adminAuth = new AdminAuth();
        
        // Refresh session on user activity
        let activityTimeout;
        const resetActivityTimeout = () => {
            clearTimeout(activityTimeout);
            activityTimeout = setTimeout(() => {
                if (window.adminAuth) {
                    window.adminAuth.refreshSession();
                }
            }, 1000);
        };
        
        // Monitor user activity
        ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart'].forEach(event => {
            document.addEventListener(event, resetActivityTimeout, true);
        });
    }
});

// Add CSS for notifications
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    .logout-btn {
        background: none;
        border: none;
        color: white;
        cursor: pointer;
        padding: 8px;
        border-radius: 4px;
        transition: background-color 0.2s;
    }
    
    .logout-btn:hover {
        background-color: rgba(255, 255, 255, 0.1);
    }
    
    .notification-content {
        display: flex;
        align-items: center;
        justify-content: space-between;
    }
    
    .notification-close {
        background: none;
        border: none;
        color: white;
        font-size: 20px;
        cursor: pointer;
        margin-left: 15px;
        padding: 0;
        line-height: 1;
    }
    
    .notification-close:hover {
        opacity: 0.8;
    }
`;
document.head.appendChild(style); 