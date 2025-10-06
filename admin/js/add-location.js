// Add Location System
import { initializeApp } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-app.js";
import { getFirestore, collection, addDoc, getDocs, query, where, orderBy } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-firestore.js";

// Firebase configuration
const firebaseConfig = {
    apiKey: "AIzaSyB8Xi8J7t3wRSy1TeIxiGFz-Is6U0zDFV5",
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
const saveBtn = document.getElementById('save-location-btn');
const clearBtn = document.getElementById('clear-fields-btn');
const arLocationSelect = document.getElementById('arlocation-select');
const locationName = document.getElementById('location-name');
const buildingSelect = document.getElementById('building-select');
const floorSelect = document.getElementById('floor-select');
const locationX = document.getElementById('location-x');
const locationY = document.getElementById('location-y');
const locationZ = document.getElementById('location-z');
const imageInput = document.getElementById('location-image');
const imageLabel = document.querySelector('label[for="location-image"]');

// State
let buildings = [];
let arLocations = [];

// Validation rules
const locationValidationRules = {
    name: { required: true, minLength: 2, maxLength: 100 },
    arLocation: { required: true },
    building: { required: true },
    floor: { required: true },
    x: { required: true, number: true },
    y: { required: true, number: true },
    z: { required: true, number: true }
};

// Initialize page
document.addEventListener('DOMContentLoaded', async () => {
    try {
        await checkAuth();
        await loadBuildings();
        await loadARLocations();
        setupEventListeners();
        setupFormValidation();
    } catch (error) {
        window.errorHandler.handleError(error, 'Add Location Initialization');
    }
});

// Check authentication
async function checkAuth() {
    const isLoggedIn = sessionStorage.getItem('adminLoggedIn');
    const adminEmail = sessionStorage.getItem('adminEmail');
    if (!isLoggedIn || !adminEmail) {
        window.location.href = 'index.html';
        return false;
    }
    return true;
}

// Setup event listeners
function setupEventListeners() {
    // Save button
    saveBtn.addEventListener('click', handleSaveLocation);
    
    // Clear button
    clearBtn.addEventListener('click', handleClearFields);
    
    // Building selection
    buildingSelect.addEventListener('change', handleBuildingChange);
    
    // AR location selection
    arLocationSelect.addEventListener('change', handleARLocationChange);
    
    // Image upload
    imageInput.addEventListener('change', handleImageUpload);
}

// Setup form validation
function setupFormValidation() {
    const form = document.querySelector('.location-form');
    if (form) {
        window.validationUtils.addRealTimeValidation(form, locationValidationRules);
    }
}

// Load buildings from Firebase
async function loadBuildings() {
    try {
        const buildingsSnapshot = await getDocs(collection(db, 'buildings'));
        buildings = [];
        
        buildingsSnapshot.forEach(doc => {
            buildings.push({
                id: doc.id,
                ...doc.data()
            });
        });
        
        // Sort by name
        buildings.sort((a, b) => a.name.localeCompare(b.name));
        
        // Populate building select
        buildingSelect.innerHTML = '<option value="">Select building...</option>' +
            buildings.map(building => `<option value="${building.id}">${building.name}</option>`).join('');
        
    } catch (error) {
        window.errorHandler.handleError(error, 'Loading Buildings');
    }
}

// Load AR locations from Firebase
async function loadARLocations() {
    try {
        const arLocationsSnapshot = await getDocs(collection(db, 'ar_locations'));
        arLocations = [];
        
        arLocationsSnapshot.forEach(doc => {
            arLocations.push({
                id: doc.id,
                ...doc.data()
            });
        });
        
        // Sort by name
        arLocations.sort((a, b) => a.name.localeCompare(b.name));
        
        // Populate AR location select
        arLocationSelect.innerHTML = '<option value="">Select coordinates...</option>' +
            arLocations.map(location => `<option value="${location.id}">${location.name}</option>`).join('');
        
    } catch (error) {
        window.errorHandler.handleError(error, 'Loading AR Locations');
    }
}

// Handle building change
function handleBuildingChange() {
    const selectedBuildingId = buildingSelect.value;
    const selectedBuilding = buildings.find(b => b.id === selectedBuildingId);
    
    // Clear floor selection
    floorSelect.innerHTML = '<option value="">Select floor...</option>';
    floorSelect.disabled = true;
    
    if (selectedBuilding && selectedBuilding.floors) {
        // Enable floor selection
        floorSelect.disabled = false;
        
        // Add floor options
        for (let i = 1; i <= selectedBuilding.floors; i++) {
            const option = document.createElement('option');
            option.value = i;
            option.textContent = `Floor ${i}`;
            floorSelect.appendChild(option);
        }
    }
}

// Handle AR location change
function handleARLocationChange() {
    const selectedLocationId = arLocationSelect.value;
    const selectedLocation = arLocations.find(l => l.id === selectedLocationId);
    
    if (selectedLocation && selectedLocation.coordinates) {
        // Enable coordinate inputs
        locationX.disabled = false;
        locationY.disabled = false;
        locationZ.disabled = false;
        
        // Set coordinate values
        locationX.value = selectedLocation.coordinates.x || '';
        locationY.value = selectedLocation.coordinates.y || '';
        locationZ.value = selectedLocation.coordinates.z || '';
    } else {
        // Disable coordinate inputs
        locationX.disabled = true;
        locationY.disabled = true;
        locationZ.disabled = true;
        
        // Clear coordinate values
        locationX.value = '';
        locationY.value = '';
        locationZ.value = '';
    }
}

// Handle image upload
function handleImageUpload(event) {
    const file = event.target.files[0];
    if (!file) return;
    
    // Validate file
    const validation = window.validationUtils.validateFile(file, {
        maxSize: 5, // 5MB
        allowedTypes: ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
    });
    
    if (!validation.valid) {
        window.errorHandler.showError(validation.message);
        event.target.value = '';
        return;
    }
    
    // Validate image dimensions
    window.validationUtils.validateImageDimensions(file, 2000, 2000).then(result => {
        if (!result.valid) {
            window.errorHandler.showError(result.message);
            event.target.value = '';
            return;
        }
        
        // Show image preview
        const reader = new FileReader();
        reader.onload = (e) => {
            // Update image label to show preview
            imageLabel.innerHTML = `
                <img src="${e.target.result}" alt="Location preview" style="max-width: 100px; max-height: 100px; object-fit: cover; border-radius: 8px;">
                <p>Click to change image</p>
            `;
        };
        reader.readAsDataURL(file);
    });
}

// Handle save location
async function handleSaveLocation() {
    try {
        const form = document.querySelector('.location-form');
        const formData = new FormData(form);
        
        const locationData = {
            name: formData.get('location-name').trim(),
            arLocationId: formData.get('arlocation-select'),
            buildingId: formData.get('building-select'),
            floor: parseInt(formData.get('floor-select')),
            coordinates: {
                x: parseFloat(formData.get('location-x')),
                y: parseFloat(formData.get('location-y')),
                z: parseFloat(formData.get('location-z'))
            },
            image: '',
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString()
        };
        
        // Validate form
        const validation = window.validationUtils.validateForm(form, locationValidationRules);
        if (!validation.isValid) {
            window.validationUtils.showFormErrors(form, validation.errors);
            return;
        }
        
        // Handle image upload
        if (imageInput.files[0]) {
            const file = imageInput.files[0];
            const reader = new FileReader();
            
            reader.onload = async (e) => {
                locationData.image = e.target.result;
                await saveLocationToFirebase(locationData);
            };
            
            reader.readAsDataURL(file);
        } else {
            await saveLocationToFirebase(locationData);
        }
        
    } catch (error) {
        window.errorHandler.handleError(error, 'Saving Location');
    }
}

// Save location to Firebase
async function saveLocationToFirebase(locationData) {
    try {
        // Show loading state
        saveBtn.disabled = true;
        saveBtn.textContent = 'Saving...';
        
        // Add to Firebase
        await addDoc(collection(db, 'locations'), locationData);
        
        // Show success message
        window.errorHandler.showSuccess('Location added successfully!');
        
        // Reset form
        handleClearFields();
        
    } catch (error) {
        window.errorHandler.handleError(error, 'Saving Location to Firebase');
    } finally {
        // Reset button state
        saveBtn.disabled = false;
        saveBtn.textContent = 'Add as new waypoint';
    }
}

// Handle clear fields
function handleClearFields() {
    const form = document.querySelector('.location-form');
    form.reset();
    
    // Clear validation
    window.validationUtils.clearFormValidation(form);
    
    // Reset image label
    imageLabel.innerHTML = `
        <svg width="56" height="56" viewBox="0 0 24 24" fill="none">
            <rect x="2" y="2" width="20" height="20" rx="4" fill="#d3d3d3"/>
            <path d="M8 15l2.5-3 2.5 3 3.5-4.5L20 17H4l4-5z" stroke="#206233" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <circle cx="9" cy="9" r="2" stroke="#206233" stroke-width="2"/>
        </svg>
    `;
    
    // Disable coordinate inputs
    locationX.disabled = true;
    locationY.disabled = true;
    locationZ.disabled = true;
    
    // Disable floor selection
    floorSelect.disabled = true;
    floorSelect.innerHTML = '<option value="">Select floor...</option>';
}

// Go back to admin tools
function goBackToAdminTools() {
    window.location.href = './admin-tools.html';
}
