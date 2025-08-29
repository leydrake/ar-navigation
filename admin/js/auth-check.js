// Authentication check for admin pages
function checkAdminAuth() {
    const isLoggedIn = sessionStorage.getItem('adminLoggedIn');
    const adminEmail = sessionStorage.getItem('adminEmail');
    if (!isLoggedIn || !adminEmail) {
        window.location.href = 'index.html';
        return false;
    }
    return true;
}

function getCurrentAdmin() {
    return {
        email: sessionStorage.getItem('adminEmail') || null,
        name: sessionStorage.getItem('adminName') || null,
        role: sessionStorage.getItem('adminRole') || null,
        permissions: JSON.parse(sessionStorage.getItem('adminPermissions') || '[]')
    };
}

function hasPermission(requiredPermission) {
    const admin = getCurrentAdmin();
    if (!admin.email) return false;
    
    // Super admin can do everything
    if (admin.role === 'super_admin') return true;
    
    // Events admin can access events
    if (admin.role === 'events_admin' && requiredPermission === 'events') return true;
    
    // Check specific permissions for other roles
    return admin.permissions.includes(requiredPermission);
}

function requirePermission(requiredPermission) {
    if (!checkAdminAuth()) return false;
    if (!hasPermission(requiredPermission)) {
        alert('You do not have permission to access this page.');
        window.location.href = 'admin-tools.html';
        return false;
    }
    return true;
}

// Logout function
function adminLogout() {
    sessionStorage.removeItem('adminLoggedIn');
    sessionStorage.removeItem('adminEmail');
    sessionStorage.removeItem('adminName');
    sessionStorage.removeItem('adminRole');
    sessionStorage.removeItem('adminPermissions');
    window.location.href = 'index.html';
}

// Check auth on page load
document.addEventListener('DOMContentLoaded', function() {
    checkAdminAuth();
}); 