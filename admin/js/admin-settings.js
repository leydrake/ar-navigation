import { initializeApp } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-app.js";
import { getFirestore, collection, getDocs, addDoc, deleteDoc, doc, updateDoc, getDoc, writeBatch } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-firestore.js";

// Firebase config
const firebaseConfig = {
    apiKey: "AIzaSyB8Xi8J7t3wRSy1TeIxiGFz-Is6U0zDFVg",
    authDomain: "navigatecampus.firebaseapp.com",
    projectId: "navigatecampus",
    storageBucket: "navigatecampus.appspot.com",
    messagingSenderId: "55012323145",
    appId: "1:55012323145:web:3408681d5a450f05b2b498",
    measurementId: "G-39WFZN3VPV"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const db = getFirestore(app);

// DOM Elements
const tabBtns = document.querySelectorAll('.tab-btn');
const tabPanels = document.querySelectorAll('.tab-panel');
const confirmModal = document.getElementById('confirmModal');
const confirmTitle = document.getElementById('confirmTitle');
const confirmMessage = document.getElementById('confirmMessage');
const confirmYes = document.getElementById('confirmYes');
const confirmNo = document.getElementById('confirmNo');

// Tab Switching
tabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
        const targetTab = btn.getAttribute('data-tab');
        
        // Remove active class from all tabs and panels
        tabBtns.forEach(b => b.classList.remove('active'));
        tabPanels.forEach(p => p.classList.remove('active'));
        
        // Add active class to clicked tab and corresponding panel
        btn.classList.add('active');
        document.getElementById(targetTab).classList.add('active');
    });
});

// Confirmation Modal
function showConfirmModal(title, message, onConfirm) {
    confirmTitle.textContent = title;
    confirmMessage.textContent = message;
    confirmModal.style.display = 'flex';
    
    confirmYes.onclick = () => {
        confirmModal.style.display = 'none';
        onConfirm();
    };
    
    confirmNo.onclick = () => {
        confirmModal.style.display = 'none';
    };
}

// Account Management
const accountForm = document.getElementById('accountForm');
const adminEmail = document.getElementById('adminEmail');
const currentPassword = document.getElementById('currentPassword');
const newPassword = document.getElementById('newPassword');
const confirmPassword = document.getElementById('confirmPassword');

// Function to verify current password
async function verifyCurrentPassword(email, password) {
    try {
        const hashedPassword = btoa(password); // Same encoding as login
        
        console.log('Verifying password for email:', email);
        console.log('Hashed password:', hashedPassword);
        
        // Use the same query approach as the login system
        const querySnapshot = await getDocs(
            collection(db, "admin_credentials")
        );
        
        console.log('Found', querySnapshot.docs.length, 'admin documents');
        
        // Find the admin with matching email and password
        for (const docSnap of querySnapshot.docs) {
            const data = docSnap.data();
            console.log('Checking document:', docSnap.id, 'Email:', data.email, 'Password match:', data.password === hashedPassword);
            if (data.email === email && data.password === hashedPassword) {
                console.log('Password verification successful!');
                return true;
            }
        }
        console.log('Password verification failed - no matching admin found');
        return false;
    } catch (error) {
        console.error('Error verifying current password:', error);
        return false;
    }
}

// Function to update password in Firebase
async function updatePasswordInFirebase(newPassword) {
    try {
        const hashedNewPassword = btoa(newPassword); // Hash the new password
        const currentAdminEmail = sessionStorage.getItem('adminEmail');
        
        if (!currentAdminEmail) {
            console.error('No admin email found in session');
            return false;
        }
        
        // Find the current admin's document
        const querySnapshot = await getDocs(
            collection(db, "admin_credentials")
        );
        
        let adminDocId = null;
        for (const docSnap of querySnapshot.docs) {
            const data = docSnap.data();
            if (data.email === currentAdminEmail) {
                adminDocId = docSnap.id;
                break;
            }
        }
        
        if (!adminDocId) {
            console.error('Admin document not found');
            return false;
        }
        
        await updateDoc(doc(db, "admin_credentials", adminDocId), {
            password: hashedNewPassword,
            lastUpdated: new Date()
        });
        return true;
    } catch (error) {
        console.error('Error updating password:', error);
        return false;
    }
}

// Load current admin email
async function loadAdminEmail() {
    try {
        // Get current admin email from sessionStorage
        const currentAdminEmail = sessionStorage.getItem('adminEmail');
        if (currentAdminEmail) {
            adminEmail.value = currentAdminEmail;
        } else {
            console.error('No admin email found in session');
        }
    } catch (error) {
        console.error('Error loading admin email:', error);
    }
}

// Function to check password strength
function checkPasswordStrength(password) {
    let strength = 0;
    let requirements = {
        length: password.length >= 8,
        lowercase: /[a-z]/.test(password),
        uppercase: /[A-Z]/.test(password),
        number: /[0-9]/.test(password),
        special: /[^A-Za-z0-9]/.test(password)
    };
    
    // Count met requirements
    Object.values(requirements).forEach(met => {
        if (met) strength++;
    });
    
    // Determine strength level
    let strengthLevel = 'weak';
    if (strength >= 4) strengthLevel = 'strong';
    else if (strength >= 2) strengthLevel = 'medium';
    
    return { 
        strength: strengthLevel, 
        score: strength, 
        requirements: requirements 
    };
}

// Function to update password strength indicator
function updatePasswordStrength(password) {
    const strengthResult = checkPasswordStrength(password);
    
    // Update strength bars
    const bars = [
        document.getElementById('strengthBar1'),
        document.getElementById('strengthBar2'),
        document.getElementById('strengthBar3')
    ];
    
    const strengthText = document.getElementById('strengthText');
    
    // Reset all bars
    bars.forEach(bar => {
        bar.className = 'strength-bar';
    });
    
    // Update bars based on strength
    if (password.length > 0) {
        if (strengthResult.strength === 'weak') {
            bars[0].classList.add('active');
            strengthText.textContent = 'Weak';
            strengthText.className = 'strength-text weak';
        } else if (strengthResult.strength === 'medium') {
            bars[0].classList.add('medium');
            bars[1].classList.add('medium');
            strengthText.textContent = 'Medium';
            strengthText.className = 'strength-text medium';
        } else if (strengthResult.strength === 'strong') {
            bars[0].classList.add('strong');
            bars[1].classList.add('strong');
            bars[2].classList.add('strong');
            strengthText.textContent = 'Strong';
            strengthText.className = 'strength-text strong';
        }
    } else {
        strengthText.textContent = 'Enter a password';
        strengthText.className = 'strength-text';
    }
    
    // Update requirements checklist
    updateRequirementsChecklist(strengthResult.requirements);
}

// Function to update requirements checklist progressively
function updateRequirementsChecklist(requirements) {
    const requirementsContainer = document.getElementById('passwordRequirements');
    const currentRequirement = document.getElementById('currentRequirement');
    
    if (!requirementsContainer || !currentRequirement) return;
    
    // Define the order of requirements to show
    const requirementOrder = [
        { key: 'length', text: 'At least 8 characters' },
        { key: 'lowercase', text: 'One lowercase letter' },
        { key: 'uppercase', text: 'One uppercase letter' },
        { key: 'number', text: 'One number' },
        { key: 'special', text: 'One special character' }
    ];
    
    // Find the first unmet requirement
    let firstUnmet = null;
    for (const req of requirementOrder) {
        if (!requirements[req.key]) {
            firstUnmet = req;
            break;
        }
    }
    
    // Show/hide requirements container
    if (firstUnmet) {
        requirementsContainer.style.display = 'block';
        currentRequirement.querySelector('.requirement-text').textContent = firstUnmet.text;
        currentRequirement.classList.remove('valid');
        currentRequirement.querySelector('.requirement-icon').textContent = '❌';
    } else {
        // All requirements met
        requirementsContainer.style.display = 'block';
        currentRequirement.querySelector('.requirement-text').textContent = 'All requirements met!';
        currentRequirement.classList.add('valid');
        currentRequirement.querySelector('.requirement-icon').textContent = '✅';
    }
}

// Function to validate form fields using centralized validation
function validateForm() {
    const form = document.getElementById('accountForm');
    const validationRules = {
        email: { required: true, email: true },
        currentPassword: { required: true },
        newPassword: { required: true, password: true, minLength: 6 },
        confirmPassword: { required: true, passwordMatch: 'newPassword' }
    };
    
    const validation = window.validationUtils.validateForm(form, validationRules);
    if (!validation.isValid) {
        window.validationUtils.showFormErrors(form, validation.errors);
    }
    
    return validation.isValid;
}

// Function to clear all field errors
function clearAllFieldErrors() {
    const errorMessages = document.querySelectorAll('.error-message');
    errorMessages.forEach(msg => msg.remove());
    
    const errorFields = document.querySelectorAll('.form-group.error');
    errorFields.forEach(field => field.classList.remove('error'));
}

// Function to show field error
function showFieldError(input, message) {
    const formGroup = input.closest('.form-group');
    formGroup.classList.add('error');
    
    // Check if error message already exists
    let errorMsg = formGroup.querySelector('.error-message');
    if (!errorMsg) {
        errorMsg = document.createElement('div');
        errorMsg.className = 'error-message';
        formGroup.appendChild(errorMsg);
    }
    errorMsg.textContent = message;
}

// Function to show field success
function showFieldSuccess(input) {
    const formGroup = input.closest('.form-group');
    formGroup.classList.remove('error');
    formGroup.classList.add('success');
    
    const errorMsg = formGroup.querySelector('.error-message');
    if (errorMsg) errorMsg.remove();
}

// Password strength indicator is now built into the HTML

// Add event listeners for real-time validation
function addFormValidationListeners() {
    // Email validation
    adminEmail.addEventListener('blur', () => {
        const email = adminEmail.value.trim();
        if (email && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            showFieldSuccess(adminEmail);
        }
    });
    
    // Current password validation
    currentPassword.addEventListener('input', () => {
        if (currentPassword.value.length > 0) {
            showFieldSuccess(currentPassword);
        }
    });
    
    // New password strength checking
    newPassword.addEventListener('input', () => {
        updatePasswordStrength(newPassword.value);
        if (newPassword.value.length >= 6) {
            showFieldSuccess(newPassword);
        }
        
        // Only validate confirm password if both fields have content
        if (confirmPassword.value && newPassword.value) {
            if (newPassword.value === confirmPassword.value) {
                showFieldSuccess(confirmPassword);
            } else {
                showFieldError(confirmPassword, 'Passwords do not match');
            }
        }
    });
    
    // Confirm password validation
    confirmPassword.addEventListener('input', () => {
        if (confirmPassword.value && newPassword.value) {
            if (confirmPassword.value === newPassword.value) {
                showFieldSuccess(confirmPassword);
            } else {
                showFieldError(confirmPassword, 'Passwords do not match');
            }
        } else if (!confirmPassword.value) {
            // Clear error when confirm password is empty
            showFieldSuccess(confirmPassword);
        }
    });
}

accountForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    // Clear any previous error messages
    clearAllFieldErrors();
    
    const email = adminEmail.value.trim();
    const currentPass = currentPassword.value;
    const newPass = newPassword.value;
    const confirmPass = confirmPassword.value;
    
    // Validate required fields
    if (!currentPass) {
        showFieldError(currentPassword, 'Please enter your current password');
        return;
    }
    
    if (!newPass) {
        showFieldError(newPassword, 'Please enter a new password');
        return;
    }
    
    if (!confirmPass) {
        showFieldError(confirmPassword, 'Please confirm your new password');
        return;
    }
    
    // Check if passwords match
    if (newPass !== confirmPass) {
        showFieldError(confirmPassword, 'Passwords do not match');
        showFieldError(newPassword, 'Passwords do not match');
        return;
    }
    
    // Check if new password is different from current
    if (currentPass === newPass) {
        showFieldError(newPassword, 'New password must be different from current password');
        return;
    }
    
    // Check password strength and requirements
    const strengthResult = checkPasswordStrength(newPass);
    if (strengthResult.strength === 'weak') {
        showFieldError(newPassword, 'Password is too weak. Please meet all requirements.');
        return;
    }
    
    // Check if all requirements are met
    const allRequirementsMet = Object.values(strengthResult.requirements).every(req => req);
    if (!allRequirementsMet) {
        showFieldError(newPassword, 'Please meet all password requirements.');
        return;
    }
    
    // Show loading state
    const submitBtn = accountForm.querySelector('.save-btn');
    const originalText = submitBtn.textContent;
    submitBtn.textContent = 'Updating...';
    submitBtn.disabled = true;
    submitBtn.classList.add('loading');
    
    try {
        // Verify current password
        const isCurrentPasswordValid = await verifyCurrentPassword(email, currentPass);
        
        if (!isCurrentPasswordValid) {
            showFieldError(currentPassword, 'Current password is incorrect');
            alert('❌ Current password is incorrect. Please check and try again.');
            return;
        }
        
        // Update password in Firebase
        const updateSuccess = await updatePasswordInFirebase(newPass);
        
        if (updateSuccess) {
            // Show success message
            alert('✅ Password updated successfully! You can now use your new password to log in.');
            
            // Clear form and show success states
            currentPassword.value = '';
            newPassword.value = '';
            confirmPassword.value = '';
            showFieldSuccess(currentPassword);
            showFieldSuccess(newPassword);
            showFieldSuccess(confirmPassword);
            
            // Clear password strength indicator
            updatePasswordStrength('');
        } else {
            showFieldError(newPassword, 'Failed to update password');
            alert('❌ Failed to update password. Please try again or contact support if the problem persists.');
        }
        
    } catch (error) {
        console.error('Error updating account:', error);
        showFieldError(newPassword, 'An error occurred');
        alert('❌ An error occurred while updating the password. Please try again or contact support.');
    } finally {
        // Reset button state
        submitBtn.textContent = originalText;
        submitBtn.disabled = false;
        submitBtn.classList.remove('loading');
    }
});

// Load admin email when page loads
loadAdminEmail();

// Initialize enhanced password change functionality
addFormValidationListeners();

// Event Management
const defaultEventImage = document.getElementById('defaultEventImage');
const defaultImagePreview = document.getElementById('defaultImagePreview');
const requireApproval = document.getElementById('requireApproval');
const showEvents = document.getElementById('showEvents');
const exportEventsBtn = document.getElementById('exportEvents');
const importEventsBtn = document.getElementById('importEvents');
const importFile = document.getElementById('importFile');
const clearEventsBtn = document.getElementById('clearEvents');

// Default event image preview
defaultEventImage.addEventListener('change', (e) => {
    const file = e.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = (evt) => {
            defaultImagePreview.src = evt.target.result;
            defaultImagePreview.style.display = 'block';
        };
        reader.readAsDataURL(file);
    }
});

// Export events
exportEventsBtn.addEventListener('click', async () => {
    try {
        const querySnapshot = await getDocs(collection(db, "events"));
        const events = [];
        querySnapshot.forEach((doc) => {
            events.push({ id: doc.id, ...doc.data() });
        });
        
        const dataStr = JSON.stringify(events, null, 2);
        const dataBlob = new Blob([dataStr], { type: 'application/json' });
        const url = URL.createObjectURL(dataBlob);
        
        const link = document.createElement('a');
        link.href = url;
        link.download = 'events-export.json';
        link.click();
        
        URL.revokeObjectURL(url);
        alert('Events exported successfully!');
    } catch (error) {
        alert('Error exporting events: ' + error.message);
    }
});

// Import events
importEventsBtn.addEventListener('click', () => {
    importFile.click();
});

importFile.addEventListener('change', async (e) => {
    const file = e.target.files[0];
    if (!file) return;
    
    showConfirmModal(
        'Import Events',
        'This will add all events from the file. Continue?',
        async () => {
            try {
                const text = await file.text();
                const events = JSON.parse(text);
                
                const batch = writeBatch(db);
                events.forEach(event => {
                    const { id, ...eventData } = event;
                    const docRef = doc(collection(db, "events"));
                    batch.set(docRef, eventData);
                });
                
                await batch.commit();
                alert('Events imported successfully!');
                importFile.value = '';
            } catch (error) {
                alert('Error importing events: ' + error.message);
            }
        }
    );
});

// Clear all events
clearEventsBtn.addEventListener('click', () => {
    showConfirmModal(
        'Clear All Events',
        'This will permanently delete ALL events. This action cannot be undone. Are you sure?',
        async () => {
            try {
                const querySnapshot = await getDocs(collection(db, "events"));
                const batch = writeBatch(db);
                querySnapshot.forEach((doc) => {
                    batch.delete(doc.ref);
                });
                await batch.commit();
                alert('All events cleared successfully!');
            } catch (error) {
                alert('Error clearing events: ' + error.message);
            }
        }
    );
});

// Appearance Settings
const siteLogo = document.getElementById('siteLogo');
const logoPreview = document.getElementById('logoPreview');
const themeRadios = document.querySelectorAll('input[name="theme"]');
const accentColor = document.getElementById('accentColor');
const saveAppearanceBtn = document.getElementById('saveAppearance');

// Logo preview
siteLogo.addEventListener('change', (e) => {
    const file = e.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = (evt) => {
            logoPreview.src = evt.target.result;
            logoPreview.style.display = 'block';
        };
        reader.readAsDataURL(file);
    }
});

// Save appearance
saveAppearanceBtn.addEventListener('click', () => {
    const selectedTheme = document.querySelector('input[name="theme"]:checked').value;
    const color = accentColor.value;
    
    // Save to localStorage for now (in a real app, save to Firestore)
    localStorage.setItem('appTheme', selectedTheme);
    localStorage.setItem('accentColor', color);
    
    alert('Appearance settings saved!');
});

// Security Settings
const allowPublicAdd = document.getElementById('allowPublicAdd');
const allowPublicEdit = document.getElementById('allowPublicEdit');
const adminList = document.getElementById('adminList');
const addAdminBtn = document.getElementById('addAdmin');
const saveSecurityBtn = document.getElementById('saveSecurity');

// Load admin list
function loadAdminList() {
    // Get current admin information from sessionStorage
    const currentAdminEmail = sessionStorage.getItem('adminEmail');
    const currentAdminName = sessionStorage.getItem('adminName');
    const currentAdminRole = sessionStorage.getItem('adminRole');
    
    // Check if admin is logged in
    if (!currentAdminEmail) {
        adminList.innerHTML = `
            <div style="color: #e74c3c; font-style: italic;">
                <div>No admin logged in</div>
            </div>
        `;
        return;
    }
    
    // Display current admin information
    const roleDisplay = currentAdminRole ? ` (${currentAdminRole.replace('_', ' ').toUpperCase()})` : '';
    const nameDisplay = currentAdminName ? `${currentAdminName} - ` : '';
    
    adminList.innerHTML = `
        <div style="color: #666; font-style: italic;">
            <div>${nameDisplay}${currentAdminEmail}${roleDisplay}</div>
        </div>
    `;
}

// Check authentication and load admin list
async function initializeAdminSettings() {
    // Check if admin is logged in
    const isLoggedIn = sessionStorage.getItem('adminLoggedIn');
    const adminEmail = sessionStorage.getItem('adminEmail');
    
    if (!isLoggedIn || !adminEmail) {
        // Redirect to login if not authenticated
        window.location.href = 'index.html';
        return;
    }
    
    // Load admin list with current admin info
    loadAdminList();
}

// Initialize when page loads
initializeAdminSettings();

// Add admin (placeholder)
addAdminBtn.addEventListener('click', () => {
    const email = prompt('Enter admin email:');
    if (email) {
        alert(`Admin ${email} added successfully! (Note: Requires Firebase Auth setup)`);
    }
});

// Save security settings
saveSecurityBtn.addEventListener('click', () => {
    const settings = {
        allowPublicAdd: allowPublicAdd.checked,
        allowPublicEdit: allowPublicEdit.checked
    };
    
    localStorage.setItem('securitySettings', JSON.stringify(settings));
    alert('Security settings saved!');
});

// Notification Settings
const emailNewEvents = document.getElementById('emailNewEvents');
const emailEventChanges = document.getElementById('emailEventChanges');
const emailDeletions = document.getElementById('emailDeletions');
const notificationEmail = document.getElementById('notificationEmail');
const saveNotificationsBtn = document.getElementById('saveNotifications');

// Save notification settings
saveNotificationsBtn.addEventListener('click', () => {
    const settings = {
        emailNewEvents: emailNewEvents.checked,
        emailEventChanges: emailEventChanges.checked,
        emailDeletions: emailDeletions.checked,
        notificationEmail: notificationEmail.value
    };
    
    localStorage.setItem('notificationSettings', JSON.stringify(settings));
    alert('Notification settings saved!');
});

// About Section
const lastUpdated = document.getElementById('lastUpdated');

// Set last updated date
lastUpdated.textContent = new Date().toLocaleDateString();

// Load saved settings on page load
function loadSavedSettings() {
    // Load theme
    const savedTheme = localStorage.getItem('appTheme');
    if (savedTheme) {
        document.querySelector(`input[name="theme"][value="${savedTheme}"]`).checked = true;
    }
    
    // Load accent color
    const savedColor = localStorage.getItem('accentColor');
    if (savedColor) {
        accentColor.value = savedColor;
    }
    
    // Load security settings
    const securitySettings = localStorage.getItem('securitySettings');
    if (securitySettings) {
        const settings = JSON.parse(securitySettings);
        allowPublicAdd.checked = settings.allowPublicAdd;
        allowPublicEdit.checked = settings.allowPublicEdit;
    }
    
    // Load notification settings
    const notificationSettings = localStorage.getItem('notificationSettings');
    if (notificationSettings) {
        const settings = JSON.parse(notificationSettings);
        emailNewEvents.checked = settings.emailNewEvents;
        emailEventChanges.checked = settings.emailEventChanges;
        emailDeletions.checked = settings.emailDeletions;
        notificationEmail.value = settings.notificationEmail || '';
    }
}

// Initialize
loadSavedSettings();

// Close modal when clicking outside
confirmModal.addEventListener('click', (e) => {
    if (e.target === confirmModal) {
        confirmModal.style.display = 'none';
    }
}); 