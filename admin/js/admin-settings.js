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

accountForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    // Get form values
    const email = adminEmail.value.trim();
    const currentPass = currentPassword.value;
    const newPass = newPassword.value;
    const confirmPass = confirmPassword.value;
    
    // Validation
    if (!email || !currentPass || !newPass || !confirmPass) {
        alert('Please fill in all fields!');
        return;
    }
    
    if (newPass !== confirmPass) {
        alert('New passwords do not match!');
        return;
    }
    
    if (newPass.length < 6) {
        alert('Password must be at least 6 characters long!');
        return;
    }
    
    try {
        // Get current admin credentials from Firestore
        const adminDoc = await getDoc(doc(db, "admin_credentials", "admin"));
        
        if (!adminDoc.exists()) {
            alert('Admin credentials not found!');
            return;
        }
        
        const adminData = adminDoc.data();
        const currentHashedPassword = btoa(currentPass); // Simple base64 encoding
        
        // Verify current password
        if (adminData.password !== currentHashedPassword) {
            alert('Current password is incorrect!');
            return;
        }
        
        // Verify current email
        if (adminData.email !== email) {
            alert('Email does not match current admin email!');
            return;
        }
        
        // Update admin credentials in Firestore
        const newHashedPassword = btoa(newPass);
        await updateDoc(doc(db, "admin_credentials", "admin"), {
            email: email,
            password: newHashedPassword,
            lastUpdated: new Date()
        });
        
        alert('Password updated successfully!');
        accountForm.reset();
        
        // Update session storage if email changed
        if (sessionStorage.getItem('adminEmail') !== email) {
            sessionStorage.setItem('adminEmail', email);
        }
        
    } catch (error) {
        console.error('Error updating password:', error);
        alert('Error updating password: ' + error.message);
    }
});

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
    adminList.innerHTML = `
        <div style="color: #666; font-style: italic;">
            <div>admin@navigatecampus.com (Primary Admin)</div>
            <div>support@navigatecampus.com (Support Admin)</div>
        </div>
    `;
}

loadAdminList();

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
async function loadSavedSettings() {
    // Load current admin email from Firestore
    try {
        const adminDoc = await getDoc(doc(db, "admin_credentials", "admin"));
        if (adminDoc.exists()) {
            const adminData = adminDoc.data();
            adminEmail.value = adminData.email || '';
        }
    } catch (error) {
        console.error('Error loading admin email:', error);
    }
    
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

// Check authentication
function checkAuth() {
    const isLoggedIn = sessionStorage.getItem('adminLoggedIn');
    const adminEmail = sessionStorage.getItem('adminEmail');
    
    if (!isLoggedIn || !adminEmail) {
        window.location.href = 'index.html';
        return false;
    }
    return true;
}

// Initialize
if (checkAuth()) {
    loadSavedSettings();
}

// Close modal when clicking outside
confirmModal.addEventListener('click', (e) => {
    if (e.target === confirmModal) {
        confirmModal.style.display = 'none';
    }
}); 