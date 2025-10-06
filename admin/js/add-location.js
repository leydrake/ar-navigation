// Add Location System
import { initializeApp } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-app.js";
import { getFirestore, collection, addDoc, getDocs, query, where, orderBy, doc, getDoc, setDoc, deleteDoc } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-firestore.js";

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
const archiveBtn = document.getElementById('archiveBtn');
const archiveModal = document.getElementById('archiveModal');
const archiveList = document.getElementById('archiveList');
const closeArchiveBtn = document.getElementById('closeArchiveBtn');
const selectAllArchiveBtn = document.getElementById('selectAllArchiveBtn');
const deleteSelectedArchiveBtn = document.getElementById('deleteSelectedArchiveBtn');
const clearAllArchiveBtn = document.getElementById('clearAllArchiveBtn');

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
    // Attach UI event listeners first so UI remains interactive even if data loads fail
    setupEventListeners();
    try {
        await checkAuth();
        await loadBuildings();
        await loadARLocations();
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
    
    // Archive modal
    if (archiveBtn) archiveBtn.addEventListener('click', openArchiveModal);
    if (closeArchiveBtn) closeArchiveBtn.addEventListener('click', () => archiveModal.style.display = 'none');
    if (selectAllArchiveBtn) selectAllArchiveBtn.addEventListener('click', selectAllArchiveItems);
    if (deleteSelectedArchiveBtn) deleteSelectedArchiveBtn.addEventListener('click', deleteSelectedArchiveItems);
    if (clearAllArchiveBtn) clearAllArchiveBtn.addEventListener('click', clearAllArchiveItems);
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

// Load AR locations from Firebase (ARLocations collection, only positions)
async function loadARLocations() {
    try {
        const arLocationsSnapshot = await getDocs(collection(db, 'ARLocations'));
        arLocations = [];
        
        arLocationsSnapshot.forEach(docSnap => {
            const data = docSnap.data() || {};
            const name = String(data.Name ?? data.name ?? docSnap.id);
            const x = Number(data.PositionX ?? data.x ?? 0);
            const y = Number(data.PositionY ?? data.y ?? 0);
            const z = Number(data.PositionZ ?? data.z ?? 0);
            arLocations.push({ id: docSnap.id, name, x, y, z });
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
async function handleBuildingChange() {
    const selectedBuildingId = buildingSelect.value;
    
    // Reset floors UI
    floorSelect.innerHTML = '<option value="">Select floor...</option>';
    floorSelect.disabled = true;
    
    if (!selectedBuildingId) return;
    
    try {
        // Fetch floors linked to the selected building
        const floorsSnap = await getDocs(query(collection(db, 'floors'), where('buildingId', '==', selectedBuildingId)));
        const floors = [];
        floorsSnap.forEach(docSnap => {
            const data = docSnap.data() || {};
            floors.push({ id: docSnap.id, name: String(data.name || `Floor`), number: Number.isFinite(data.number) ? data.number : null });
        });
        // Sort by number then name
        floors.sort((a, b) => {
            const an = a.number ?? Number.POSITIVE_INFINITY;
            const bn = b.number ?? Number.POSITIVE_INFINITY;
            if (an !== bn) return an - bn;
            return a.name.localeCompare(b.name);
        });
        // Populate options
        for (const f of floors) {
            const option = document.createElement('option');
            option.value = f.id;
            option.textContent = f.number != null ? `Floor ${f.number} — ${f.name}` : f.name;
            floorSelect.appendChild(option);
        }
        // Enable if we have at least one floor
        if (floors.length > 0) {
            floorSelect.disabled = false;
        }
    } catch (error) {
        window.errorHandler.handleError(error, 'Loading Floors for Building');
    }
}

// Handle AR location change
function handleARLocationChange() {
    const selectedLocationId = arLocationSelect.value;
    const selectedLocation = arLocations.find(l => l.id === selectedLocationId);
    
    if (selectedLocation) {
        // Keep coordinate inputs disabled per new requirement
        locationX.disabled = true;
        locationY.disabled = true;
        locationZ.disabled = true;

        // Set coordinate values from positions only
        locationX.value = Number.isFinite(selectedLocation.x) ? String(selectedLocation.x) : '';
        locationY.value = Number.isFinite(selectedLocation.y) ? String(selectedLocation.y) : '';
        locationZ.value = Number.isFinite(selectedLocation.z) ? String(selectedLocation.z) : '';
        
        // Mirror chosen name into the name field if empty
        if (!locationName.value) {
            const opt = arLocationSelect.options[arLocationSelect.selectedIndex];
            locationName.value = opt ? (opt.textContent || '') : '';
        }
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
        
        // Read XYZ directly from inputs since disabled fields are not included in FormData
        const xVal = Number(locationX.value);
        const yVal = Number(locationY.value);
        const zVal = Number(locationZ.value);
        if (!Number.isFinite(xVal) || !Number.isFinite(yVal) || !Number.isFinite(zVal)) {
            alert('Missing or invalid coordinates (X, Y, Z). Please select a valid AR Location.');
            return;
        }

        const locationData = {
            name: formData.get('location-name').trim(),
            arLocationId: formData.get('arlocation-select'),
            buildingId: formData.get('building-select') || null,
            floorId: formData.get('floor-select') || null,
            coordinates: {
                x: xVal,
                y: yVal,
                z: zVal
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
        
        // Add to Firebase: store in 'coordinates' collection
        const coordsDoc = {
            name: locationData.name,
            x: locationData.coordinates.x,
            y: locationData.coordinates.y,
            z: locationData.coordinates.z,
            buildingId: locationData.buildingId,
            floorId: locationData.floorId,
            createdAt: new Date().toISOString()
        };
        await addDoc(collection(db, 'coordinates'), coordsDoc);
        
        // If the location came from ARLocations, archive the source doc
        const selectedId = arLocationSelect.value || '';
        if (selectedId) {
            try {
                const srcRef = doc(db, 'ARLocations', selectedId);
                const srcSnap = await getDoc(srcRef);
                if (srcSnap.exists()) {
                    const data = srcSnap.data() || {};
                    // Write to archive collection with timestamp
                    const archiveRef = doc(collection(db, 'ARLocations_archive'));
                    await setDoc(archiveRef, { ...data, archivedAt: new Date().toISOString(), sourceId: selectedId });
                    // Remove from source
                    await deleteDoc(srcRef);
                }
            } catch (archiveErr) {
                console.warn('Archiving ARLocation failed:', archiveErr);
            }
        }
        
        // Show success message
        window.errorHandler.showSuccess('Location added successfully!');
        
        // Reset form and ensure XYZ remain disabled
        handleClearFields();
        // Reload AR coordinates to remove archived item from dropdown
        await loadARLocations();
        
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

// Archive modal logic
async function openArchiveModal() {
    try {
        archiveList.innerHTML = 'Loading...';
        archiveModal.style.display = 'flex';
        const snap = await getDocs(query(collection(db, 'ARLocations_archive'), orderBy('archivedAt', 'desc')));
        const items = [];
        snap.forEach(docSnap => {
            const d = docSnap.data() || {};
            const name = String(d.Name ?? d.name ?? docSnap.id);
            const x = d.PositionX ?? d.x ?? '-';
            const y = d.PositionY ?? d.y ?? '-';
            const z = d.PositionZ ?? d.z ?? '-';
            const time = d.archivedAt ? new Date(d.archivedAt).toLocaleString() : '';
            items.push(`<label style="display:flex;align-items:flex-start;gap:10px;padding:10px;border-bottom:1px solid #eee;">`+
                `<input type="checkbox" class="archive-checkbox" data-id="${docSnap.id}">`+
                `<div style="flex:1;">`+
                    `<div style="font-weight:600;">${name}</div>`+
                    `<div style="font-size:12px;color:#555;">X:${x} Y:${y} Z:${z}</div>`+
                    `<div style="font-size:12px;color:#888;">Archived: ${time}</div>`+
                `</div>`+
            `</label>`);
        });
        archiveList.innerHTML = items.length ? items.join('') : '<div style="padding:10px;">No archived items.</div>';
    } catch (err) {
        window.errorHandler.handleError(err, 'Open Archive Modal');
    }
}

function selectAllArchiveItems() {
    const boxes = archiveList.querySelectorAll('.archive-checkbox');
    const allChecked = Array.from(boxes).every(b => b.checked);
    boxes.forEach(b => b.checked = !allChecked);
}

async function deleteSelectedArchiveItems() {
    try {
        const boxes = Array.from(archiveList.querySelectorAll('.archive-checkbox')).filter(b => b.checked);
        if (boxes.length === 0) {
            alert('No items selected.');
            return;
        }
        if (!confirm(`Delete ${boxes.length} selected item(s)? This cannot be undone.`)) return;
        for (const box of boxes) {
            const id = box.getAttribute('data-id');
            await deleteDoc(doc(db, 'ARLocations_archive', id));
        }
        await openArchiveModal();
    } catch (err) {
        window.errorHandler.handleError(err, 'Delete Selected Archive Items');
    }
}

async function clearAllArchiveItems() {
    try {
        if (!confirm('Delete ALL archived items? This cannot be undone.')) return;
        const snap = await getDocs(collection(db, 'ARLocations_archive'));
        const ids = [];
        snap.forEach(d => ids.push(d.id));
        for (const id of ids) {
            await deleteDoc(doc(db, 'ARLocations_archive', id));
        }
        await openArchiveModal();
    } catch (err) {
        window.errorHandler.handleError(err, 'Clear All Archive Items');
    }
}

// Go back to admin tools
function goBackToAdminTools() {
    window.location.href = './admin-tools.html';
}
