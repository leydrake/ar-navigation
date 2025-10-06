// Building Management System
import { initializeApp } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-app.js";
import { getFirestore, collection, addDoc, getDocs, updateDoc, deleteDoc, doc, query, orderBy, where } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-firestore.js";

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
const loadingState = document.getElementById('loadingState');
const mainContainer = document.getElementById('mainContainer');
const buildingsList = document.getElementById('buildingsList');
const emptyState = document.getElementById('emptyState');
const searchInput = document.getElementById('searchInput');
const refreshBtn = document.getElementById('refreshBuildingsBtn');
const progressOverlay = document.getElementById('progressOverlay');
const progressText = document.getElementById('progressText');

// State
let buildings = [];
let filteredBuildings = [];

// Validation rules
const buildingValidationRules = {
    name: { required: true, minLength: 2, maxLength: 100 },
    description: { required: true, minLength: 10, maxLength: 500 },
    address: { required: true, minLength: 5, maxLength: 200 },
    floors: { required: true, number: true, min: 1, max: 50 },
    coordinates: {
        latitude: { required: true, number: true, min: -90, max: 90 },
        longitude: { required: true, number: true, min: -180, max: 180 }
    }
};

// Initialize page
document.addEventListener('DOMContentLoaded', async () => {
    try {
        await checkAuth();
        await loadBuildings();
        setupEventListeners();
        showMainContainer();
    } catch (error) {
        window.errorHandler.handleError(error, 'Building Management Initialization');
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
    // Search functionality
    searchInput.addEventListener('input', handleSearch);
    
    // Refresh button
    refreshBtn.addEventListener('click', handleRefresh);
    
    // Add building button
    const addBuildingBtn = document.querySelector('.add-building-btn');
    if (addBuildingBtn) {
        addBuildingBtn.addEventListener('click', showAddBuildingModal);
    }
}

// Load buildings from Firebase
async function loadBuildings() {
    try {
        showProgress('Loading buildings...');
        
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
        filteredBuildings = [...buildings];
        
        renderBuildings();
        hideProgress();
        
    } catch (error) {
        hideProgress();
        window.errorHandler.handleError(error, 'Loading Buildings');
    }
}

// Render buildings list
function renderBuildings() {
    if (filteredBuildings.length === 0) {
        buildingsList.style.display = 'none';
        emptyState.style.display = 'block';
        return;
    }
    
    buildingsList.style.display = 'block';
    emptyState.style.display = 'none';
    
    buildingsList.innerHTML = filteredBuildings.map(building => `
        <div class="building-card" data-building-id="${building.id}">
            <div class="building-header">
                <div class="building-info">
                    <h3 class="building-name">${building.name}</h3>
                    <p class="building-address">${building.address}</p>
                </div>
                <div class="building-actions">
                    <button class="action-btn edit-btn" onclick="editBuilding('${building.id}')" title="Edit Building">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        </svg>
                    </button>
                    <button class="action-btn delete-btn" onclick="deleteBuilding('${building.id}')" title="Delete Building">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                            <path d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        </svg>
                    </button>
                </div>
            </div>
            <div class="building-details">
                <div class="building-detail">
                    <span class="detail-label">Floors:</span>
                    <span class="detail-value">${building.floors || 'N/A'}</span>
                </div>
                <div class="building-detail">
                    <span class="detail-label">Coordinates:</span>
                    <span class="detail-value">${building.coordinates?.latitude || 'N/A'}, ${building.coordinates?.longitude || 'N/A'}</span>
                </div>
            </div>
            <div class="building-description">
                <p>${building.description || 'No description available'}</p>
            </div>
        </div>
    `).join('');
}

// Handle search
function handleSearch() {
    const searchTerm = searchInput.value.toLowerCase().trim();
    
    if (!searchTerm) {
        filteredBuildings = [...buildings];
    } else {
        filteredBuildings = buildings.filter(building => 
            building.name.toLowerCase().includes(searchTerm) ||
            building.address.toLowerCase().includes(searchTerm) ||
            building.description?.toLowerCase().includes(searchTerm)
        );
    }
    
    renderBuildings();
}

// Handle refresh
async function handleRefresh() {
    refreshBtn.style.transform = 'rotate(360deg)';
    await loadBuildings();
    setTimeout(() => {
        refreshBtn.style.transform = 'rotate(0deg)';
    }, 500);
}

// Show add building modal
function showAddBuildingModal() {
    const modal = createBuildingModal();
    document.body.appendChild(modal);
    modal.style.display = 'flex';
}

// Create building modal
function createBuildingModal(building = null) {
    const isEdit = !!building;
    const modal = document.createElement('div');
    modal.className = 'modal-overlay';
    modal.innerHTML = `
        <div class="modal-content">
            <div class="modal-header">
                <h2>${isEdit ? 'Edit Building' : 'Add New Building'}</h2>
                <button class="close-btn" onclick="closeModal(this)">&times;</button>
            </div>
            <form class="building-form" id="buildingForm">
                <div class="form-group">
                    <label for="buildingName">Building Name *</label>
                    <input type="text" id="buildingName" name="name" value="${building?.name || ''}" required>
                </div>
                <div class="form-group">
                    <label for="buildingDescription">Description *</label>
                    <textarea id="buildingDescription" name="description" rows="3" required>${building?.description || ''}</textarea>
                </div>
                <div class="form-group">
                    <label for="buildingAddress">Address *</label>
                    <input type="text" id="buildingAddress" name="address" value="${building?.address || ''}" required>
                </div>
                <div class="form-row">
                    <div class="form-group">
                        <label for="buildingFloors">Number of Floors *</label>
                        <input type="number" id="buildingFloors" name="floors" value="${building?.floors || ''}" min="1" max="50" required>
                    </div>
                </div>
                <div class="form-row">
                    <div class="form-group">
                        <label for="buildingLatitude">Latitude *</label>
                        <input type="number" id="buildingLatitude" name="latitude" value="${building?.coordinates?.latitude || ''}" step="any" required>
                    </div>
                    <div class="form-group">
                        <label for="buildingLongitude">Longitude *</label>
                        <input type="number" id="buildingLongitude" name="longitude" value="${building?.coordinates?.longitude || ''}" step="any" required>
                    </div>
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal(this)">Cancel</button>
                    <button type="submit" class="btn btn-primary">${isEdit ? 'Update Building' : 'Add Building'}</button>
                </div>
            </form>
        </div>
    `;
    
    // Add form validation
    const form = modal.querySelector('#buildingForm');
    const validationRules = {
        name: buildingValidationRules.name,
        description: buildingValidationRules.description,
        address: buildingValidationRules.address,
        floors: buildingValidationRules.floors,
        latitude: buildingValidationRules.coordinates.latitude,
        longitude: buildingValidationRules.coordinates.longitude
    };
    
    window.validationUtils.addRealTimeValidation(form, validationRules);
    
    // Handle form submission
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        await handleBuildingSubmit(form, building?.id);
    });
    
    return modal;
}

// Handle building form submission
async function handleBuildingSubmit(form, buildingId = null) {
    try {
        const formData = new FormData(form);
        const buildingData = {
            name: formData.get('name').trim(),
            description: formData.get('description').trim(),
            address: formData.get('address').trim(),
            floors: parseInt(formData.get('floors')),
            coordinates: {
                latitude: parseFloat(formData.get('latitude')),
                longitude: parseFloat(formData.get('longitude'))
            },
            updatedAt: new Date().toISOString()
        };
        
        // Validate form
        const validationRules = {
            name: buildingValidationRules.name,
            description: buildingValidationRules.description,
            address: buildingValidationRules.address,
            floors: buildingValidationRules.floors,
            latitude: buildingValidationRules.coordinates.latitude,
            longitude: buildingValidationRules.coordinates.longitude
        };
        
        const validation = window.validationUtils.validateForm(form, validationRules);
        if (!validation.isValid) {
            window.validationUtils.showFormErrors(form, validation.errors);
            return;
        }
        
        showProgress(buildingId ? 'Updating building...' : 'Adding building...');
        
        if (buildingId) {
            // Update existing building
            await updateDoc(doc(db, 'buildings', buildingId), buildingData);
            window.errorHandler.showSuccess('Building updated successfully!');
        } else {
            // Add new building
            buildingData.createdAt = new Date().toISOString();
            await addDoc(collection(db, 'buildings'), buildingData);
            window.errorHandler.showSuccess('Building added successfully!');
        }
        
        hideProgress();
        closeModal(form.closest('.modal-overlay').querySelector('.close-btn'));
        await loadBuildings();
        
    } catch (error) {
        hideProgress();
        window.errorHandler.handleError(error, 'Building Submission');
    }
}

// Edit building
function editBuilding(buildingId) {
    const building = buildings.find(b => b.id === buildingId);
    if (!building) {
        window.errorHandler.showError('Building not found');
        return;
    }
    
    const modal = createBuildingModal(building);
    document.body.appendChild(modal);
    modal.style.display = 'flex';
}

// Delete building
async function deleteBuilding(buildingId) {
    const building = buildings.find(b => b.id === buildingId);
    if (!building) {
        window.errorHandler.showError('Building not found');
        return;
    }
    
    const confirmed = confirm(`Are you sure you want to delete "${building.name}"? This action cannot be undone.`);
    if (!confirmed) return;
    
    try {
        showProgress('Deleting building...');
        await deleteDoc(doc(db, 'buildings', buildingId));
        window.errorHandler.showSuccess('Building deleted successfully!');
        hideProgress();
        await loadBuildings();
    } catch (error) {
        hideProgress();
        window.errorHandler.handleError(error, 'Building Deletion');
    }
}

// Close modal
function closeModal(closeBtn) {
    const modal = closeBtn.closest('.modal-overlay');
    modal.remove();
}

// Show main container
function showMainContainer() {
    loadingState.style.display = 'none';
    mainContainer.style.display = 'block';
}

// Show progress
function showProgress(message) {
    progressText.textContent = message;
    progressOverlay.style.display = 'flex';
}

// Hide progress
function hideProgress() {
    progressOverlay.style.display = 'none';
}

// Go back
function goBack() {
    window.location.href = 'admin-tools.html';
}

// Make functions globally available
window.editBuilding = editBuilding;
window.deleteBuilding = deleteBuilding;
window.closeModal = closeModal;
window.showAddBuildingModal = showAddBuildingModal;
