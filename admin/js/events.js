import { initializeApp } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-app.js";
import { getFirestore, collection, getDocs, addDoc, deleteDoc, doc, updateDoc, getDoc } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-firestore.js";

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
const eventsRef = collection(db, "events");
const coordinatesRef = collection(db, "coordinates");

// Permission checking functions
function checkEventsPermission() {
    console.log('checkEventsPermission: Starting permission check...');
    
    // Check if user is logged in
    const isLoggedIn = sessionStorage.getItem('adminLoggedIn');
    const adminEmail = sessionStorage.getItem('adminEmail');
    console.log('checkEventsPermission: isLoggedIn:', isLoggedIn, 'adminEmail:', adminEmail);
    
    if (!isLoggedIn || !adminEmail) {
        console.log('checkEventsPermission: Not logged in, redirecting to index');
        window.location.href = 'index.html';
        return false;
    }

    // Check role and permissions
    const role = sessionStorage.getItem('adminRole');
    const permissions = JSON.parse(sessionStorage.getItem('adminPermissions') || '[]');
    console.log('checkEventsPermission: role:', role, 'permissions:', permissions);
    
    // Super admin can do everything
    if (role === 'super_admin') {
        console.log('checkEventsPermission: Super admin detected, allowing access');
        return true;
    }
    
    // Events admin can only manage events
    if (role === 'events_admin') {
        console.log('checkEventsPermission: Events admin detected, allowing access');
        return true;
    }
    
    // Regular admin needs events permission
    if (permissions.includes('events')) {
        console.log('checkEventsPermission: Regular admin with events permission, allowing access');
        return true;
    }
    
    // No permission
    console.log('checkEventsPermission: No permission, showing alert and redirecting');
    alert('You do not have permission to manage events.');
    window.location.href = 'admin-tools.html';
    return false;
}

// Check permissions before allowing any actions
console.log('Events.js: Checking permissions...');
const permissionResult = checkEventsPermission();
console.log('Events.js: Permission check result:', permissionResult);
if (!permissionResult) {
    console.error('Events.js: Permission check failed');
    throw new Error('Insufficient permissions to access events management');
}
console.log('Events.js: Permission check passed');

// DOM elements
const eventsList = document.querySelector('.events-list');
const adminContainer = document.querySelector('.events-admin-container');
const loadingState = document.getElementById('loadingState');
const addEventBtn = document.querySelector('.add-event-btn');
const addEventModal = document.getElementById('addEventModal');
const closeModalBtn = document.getElementById('closeModalBtn');
const eventForm = document.getElementById('eventForm');
const eventImage = document.getElementById('eventImage');
const imagePreview = document.getElementById('imagePreview');
const eventName = document.getElementById('eventName');
const eventDescription = document.getElementById('eventDescription');
const eventLocation = document.getElementById('eventLocation');
const eventStartTime = document.getElementById('eventStartTime');
const eventEndTime = document.getElementById('eventEndTime');
const locationDropdown = document.getElementById('locationDropdown');

// View modal elements
const viewEventModal = document.getElementById('viewEventModal');
const closeViewModalBtn = document.getElementById('closeViewModalBtn');
const editEventBtn = document.getElementById('editEventBtn');
const deleteEventBtn = document.getElementById('deleteEventBtn');
const viewEventImage = document.getElementById('viewEventImage');
const viewEventName = document.getElementById('viewEventName');
const viewEventDescription = document.getElementById('viewEventDescription');
const viewEventLocation = document.getElementById('viewEventLocation');
const viewEventStartTime = document.getElementById('viewEventStartTime');
const viewEventEndTime = document.getElementById('viewEventEndTime');

// Locations will be fetched from coordinates collection
let locations = [];

let editingEventId = null;
let editingEventImage = '';
let viewingEventId = null;

// Check for overlapping events at the same location
async function hasTimeConflict(targetLocation, proposedStartIso, proposedEndIso, excludeEventId = null) {
    try {
        // Fetch all events for the same location
        const { query, where, getDocs } = await import("https://www.gstatic.com/firebasejs/12.0.0/firebase-firestore.js");
        const q = query(eventsRef, where('location', '==', targetLocation));
        const snapshot = await getDocs(q);
        const proposedStart = new Date(proposedStartIso).getTime();
        const proposedEnd = new Date(proposedEndIso).getTime();
        let conflict = null;
        snapshot.forEach(docSnap => {
            const data = docSnap.data();
            if (excludeEventId && docSnap.id === excludeEventId) return;
            const existingStart = data.startTime ? new Date(data.startTime).getTime() : null;
            const existingEnd = data.endTime ? new Date(data.endTime).getTime() : null;
            if (existingStart == null || existingEnd == null) return;
            // Overlap condition: start < existingEnd AND end > existingStart
            if (proposedStart < existingEnd && proposedEnd > existingStart) {
                conflict = {
                    name: data.name || data.title || 'Untitled Event',
                    start: existingStart,
                    end: existingEnd
                };
            }
        });
        return conflict;
    } catch (err) {
        console.error('Error checking time conflict:', err);
        return null;
    }
}

// Fetch locations from coordinates collection with building and floor info
async function fetchLocations() {
    try {
        const querySnapshot = await getDocs(coordinatesRef);
        locations = [];
        querySnapshot.forEach((doc) => {
            const data = doc.data();
            if (data.name) {
                const locationInfo = {
                    id: doc.id,
                    name: data.name,
                    building: data.building || 'Unknown Building',
                    floor: data.floor !== null && data.floor !== undefined ? `Floor ${data.floor}` : 'Ground Floor',
                    coordinates: {
                        x: data.x || 0,
                        y: data.y || 0,
                        z: data.z || 0
                    }
                };
                locations.push(locationInfo);
            }
        });
        
        // Sort locations by building, then floor, then name
        locations.sort((a, b) => {
            if (a.building !== b.building) {
                return a.building.localeCompare(b.building);
            }
            if (a.floor !== b.floor) {
                return a.floor.localeCompare(b.floor);
            }
            return a.name.localeCompare(b.name);
        });
        
        console.log('Fetched locations with details:', locations);
    } catch (error) {
        console.error('Error fetching locations:', error);
        // Fallback to default locations if fetch fails
        locations = [
            { id: 'fallback-1', name: 'Activity Center', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } },
            { id: 'fallback-2', name: 'AVR', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } },
            { id: 'fallback-3', name: 'Admin Building', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } },
            { id: 'fallback-4', name: 'MPG Building', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } },
            { id: 'fallback-5', name: 'BSBA Building', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } },
            { id: 'fallback-6', name: 'Pancho Hall', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } },
            { id: 'fallback-7', name: 'Library', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } },
            { id: 'fallback-8', name: 'Gymnasium', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } },
            { id: 'fallback-9', name: 'Canteen', building: 'Main Campus', floor: 'Ground Floor', coordinates: { x: 0, y: 0, z: 0 } }
        ];
    }
}

// Archive expired events automatically
async function archiveExpiredEvents() {
    try {
        const querySnapshot = await getDocs(eventsRef);
        const now = new Date();
        const archivePromises = [];
        
        querySnapshot.forEach((docSnap) => {
            const data = docSnap.data();
            const endTime = data.endTime ? new Date(data.endTime) : null;
            
            // If event has ended and is not already archived
            if (endTime && endTime < now && !data.archived) {
                console.log(`Archiving expired event: ${data.name || data.title}`);
                
                // Prepare event data for archive
                const eventData = { ...data };
                eventData.archivedAt = now.toISOString();
                eventData.archiveReason = 'expired';
                
                // Add to archive collection
                archivePromises.push(
                    addDoc(collection(db, "events_archive"), eventData)
                    .then(() => {
                        // Delete from main events collection
                        return deleteDoc(doc(db, "events", docSnap.id));
                    })
                );
            }
        });
        
        if (archivePromises.length > 0) {
            await Promise.all(archivePromises);
            console.log(`Archived ${archivePromises.length} expired events`);
        }
    } catch (error) {
        console.error('Error archiving expired events:', error);
    }
}

// Fetch and render events
async function fetchAndRenderEvents() {
    try {
        // show skeleton until data is ready
        if (loadingState && adminContainer) {
            loadingState.style.display = '';
            adminContainer.style.display = 'none';
        }
        
        // First archive expired events
        await archiveExpiredEvents();
        
        const querySnapshot = await getDocs(eventsRef);
        const events = [];
        querySnapshot.forEach((docSnap) => {
            const data = docSnap.data();
            const event = { id: docSnap.id, ...data };
            
            // All events in the main collection are active (non-archived)
            events.push(event);
        });
        
        // Sort events by start time (soonest first)
        events.sort((a, b) => {
            const aTime = a.startTime ? new Date(a.startTime) : new Date(0);
            const bTime = b.startTime ? new Date(b.startTime) : new Date(0);
            return aTime - bTime;
        });
        
        renderEvents(events);
        if (loadingState && adminContainer) {
            loadingState.style.display = 'none';
            adminContainer.style.display = '';
        }
    } catch (error) {
        console.error('Error fetching events:', error);
        eventsList.innerHTML = '<p class="error-message">Error loading events. Please try again.</p>';
        if (loadingState) loadingState.style.display = 'none';
        if (adminContainer) adminContainer.style.display = '';
    }
}

// Render events
function renderEvents(events) {
    if (events.length === 0) {
        eventsList.innerHTML = '<p class="no-events">No events found. Click "Add Event" to create your first event.</p>';
        return;
    }
    
    eventsList.innerHTML = '';
    events.forEach((event, idx) => {
        const card = document.createElement('div');
        card.className = 'event-card';
        card.setAttribute('data-id', event.id);
        card.setAttribute('data-event-id', event.id);
        
        const locationDisplay = event.locationBuilding && event.locationFloor 
            ? `${event.location} (${event.locationBuilding} • ${event.locationFloor})`
            : event.location;
            
        card.innerHTML = `
            <div class="event-img-placeholder">
                ${event.image ? `<img src="${event.image}" style="width:40px;height:40px;object-fit:cover;border-radius:8px;"/>` : `<svg width="40" height="40" viewBox="0 0 24 24" fill="none"><rect x="2" y="2" width="20" height="20" rx="4" fill="#f0f0f0"/><text x="12" y="16" text-anchor="middle" font-size="18" fill="#bbb">?</text></svg>`}
            </div>
            <div class="event-details">
                <div class="event-title">${event.name || event.title || 'Untitled Event'}</div>
                <div class="event-location">📍 ${locationDisplay}</div>
                <div class="event-time">${event.startTime ? new Date(event.startTime).toLocaleString() : 'No time set'}</div>
            </div>
        `;
        eventsList.appendChild(card);
    });
    
    // Add event listeners for card clicks (view modal)
    document.querySelectorAll('.event-card').forEach(card => {
        card.onclick = async function(e) {
            // Check permissions before allowing view
            if (!checkEventsPermission()) return;
            
            const id = this.getAttribute('data-id');
            await openViewModal(id, false);
        };
    });
}

// Modal open/close
addEventBtn.onclick = function() {
    // Check permissions before allowing add
    if (!checkEventsPermission()) return;
    
    addEventModal.style.display = 'flex';
    editingEventId = null;
    editingEventImage = '';
    eventForm.reset();
    imagePreview.style.display = 'none';
    
    // Clear location metadata
    delete eventLocation.dataset.locationId;
    delete eventLocation.dataset.locationBuilding;
    delete eventLocation.dataset.locationFloor;
    selectedLocationId = null;
};

closeModalBtn.onclick = function() {
    addEventModal.style.display = 'none';
    editingEventId = null;
    editingEventImage = '';
    eventForm.reset();
    imagePreview.style.display = 'none';
    
    // Clear location metadata
    delete eventLocation.dataset.locationId;
    delete eventLocation.dataset.locationBuilding;
    delete eventLocation.dataset.locationFloor;
    selectedLocationId = null;
};

// View modal functions
async function openViewModal(eventId, isArchived = false) {
    try {
        const eventDoc = await getDoc(doc(db, "events", eventId));
        if (eventDoc.exists()) {
            const data = eventDoc.data();
            viewingEventId = eventId;
            
            // Populate view modal with event data
            viewEventName.textContent = data.name || data.title || 'Untitled Event';
            viewEventDescription.textContent = data.description || 'No description provided';
            
            // Enhanced location display
            const locationDisplay = data.locationBuilding && data.locationFloor 
                ? `${data.location} (${data.locationBuilding} • ${data.locationFloor})`
                : data.location || 'No location specified';
            viewEventLocation.textContent = locationDisplay;
            
            viewEventStartTime.textContent = data.startTime ? new Date(data.startTime).toLocaleString() : 'No start time set';
            viewEventEndTime.textContent = data.endTime ? new Date(data.endTime).toLocaleString() : 'No end time set';
            
            if (data.image) {
                viewEventImage.src = data.image;
                viewEventImage.style.display = 'block';
            } else {
                viewEventImage.style.display = 'none';
            }
            
            // Update modal buttons based on archive status
            if (isArchived) {
                editEventBtn.style.display = 'none';
                deleteEventBtn.textContent = 'Delete Permanently';
                deleteEventBtn.className = 'delete-btn permanent-delete';
                
                // Add restore button if not already present
                if (!document.getElementById('restoreEventBtn')) {
                    const restoreBtn = document.createElement('button');
                    restoreBtn.id = 'restoreEventBtn';
                    restoreBtn.className = 'restore-btn';
                    restoreBtn.textContent = 'Restore';
                    restoreBtn.onclick = restoreEvent;
                    
                    // Insert restore button before delete button
                    deleteEventBtn.parentNode.insertBefore(restoreBtn, deleteEventBtn);
                }
            } else {
                editEventBtn.style.display = 'inline-block';
                deleteEventBtn.textContent = 'Delete';
                deleteEventBtn.className = 'delete-btn';
                
                // Remove restore button if present
                const restoreBtn = document.getElementById('restoreEventBtn');
                if (restoreBtn) {
                    restoreBtn.remove();
                }
            }
            
            viewEventModal.style.display = 'flex';
        }
    } catch (error) {
        console.error('Error loading event for view:', error);
        alert('Error loading event: ' + error.message);
    }
}

// Restore archived event
async function restoreEvent() {
    if (!viewingEventId) return;
    
    // Check permissions before allowing restore
    if (!checkEventsPermission()) return;
    
    const confirmed = confirm('Are you sure you want to restore this event?');
    if (!confirmed) return;
    
    try {
        await updateDoc(doc(db, "events", viewingEventId), {
            archived: false,
            restoredAt: new Date().toISOString()
        });
        
        viewEventModal.style.display = 'none';
        viewingEventId = null;
        
        // Refresh the events list (show active events)
        await fetchAndRenderEvents(false);
        alert('Event restored successfully!');
    } catch (error) {
        console.error('Error restoring event:', error);
        alert('Error restoring event: ' + error.message);
    }
}

// View modal event listeners
closeViewModalBtn.onclick = function() {
    viewEventModal.style.display = 'none';
    viewingEventId = null;
};

editEventBtn.onclick = async function() {
    if (!viewingEventId) return;
    
    // Check permissions before allowing edit
    if (!checkEventsPermission()) return;
    
    try {
        const eventDoc = await getDoc(doc(db, "events", viewingEventId));
        if (eventDoc.exists()) {
            const data = eventDoc.data();
            eventName.value = data.name || data.title || '';
            eventDescription.value = data.description || '';
            eventLocation.value = data.location || '';
            
            // Set location metadata for editing
            if (data.locationId) {
                eventLocation.dataset.locationId = data.locationId;
                eventLocation.dataset.locationBuilding = data.locationBuilding || '';
                eventLocation.dataset.locationFloor = data.locationFloor || '';
            }
            
            // Format datetime for input fields
            if (data.startTime) {
                const startDate = new Date(data.startTime);
                eventStartTime.value = startDate.toISOString().slice(0, 16);
            }
            if (data.endTime) {
                const endDate = new Date(data.endTime);
                eventEndTime.value = endDate.toISOString().slice(0, 16);
            }
            
            if (data.image) {
                imagePreview.src = data.image;
                imagePreview.style.display = 'block';
                editingEventImage = data.image;
            } else {
                imagePreview.style.display = 'none';
                editingEventImage = '';
            }
            editingEventId = viewingEventId;
            
            // Close view modal and open edit modal
            viewEventModal.style.display = 'none';
            addEventModal.style.display = 'flex';
        }
    } catch (error) {
        console.error('Error loading event for edit:', error);
        alert('Error loading event: ' + error.message);
    }
};

deleteEventBtn.onclick = async function() {
    if (!viewingEventId) return;
    
    // Check permissions before allowing delete
    if (!checkEventsPermission()) return;
    
    const confirmed = confirm('Are you sure you want to delete this event? It will be moved to archive.');
    if (!confirmed) return;
    
    try {
        // Get the event data first
        const eventDoc = await getDoc(doc(db, "events", viewingEventId));
        if (!eventDoc.exists()) {
            alert('Event not found!');
            return;
        }
        
        const eventData = eventDoc.data();
        
        // Add archivedAt timestamp
        const archivedData = { ...eventData };
        archivedData.archivedAt = new Date().toISOString();
        archivedData.archiveReason = 'deleted';
        
        // Add to archive collection
        await addDoc(collection(db, "events_archive"), archivedData);
        
        // Delete from main events collection
        await deleteDoc(doc(db, "events", viewingEventId));
        
        viewEventModal.style.display = 'none';
        viewingEventId = null;
        await fetchAndRenderEvents();
        alert('Event deleted and moved to archive successfully!');
    } catch (error) {
        console.error('Error deleting event:', error);
        alert('Error deleting event: ' + error.message);
    }
};

// Image preview with size validation and compression
eventImage.onchange = function(e) {
    const file = e.target.files[0];
    if (file) {
        // Check file size (max 1MB)
        const maxSize = 1024 * 1024; // 1MB in bytes
        if (file.size > maxSize) {
            alert('Image file is too large. Please select an image smaller than 1MB.');
            eventImage.value = ''; // Clear the input
            imagePreview.style.display = 'none';
            updateImageUploadText('Click to upload image');
            return;
        }
        
        // Update upload button text
        updateImageUploadText(`Selected: ${file.name}`);
        
        // Compress the image before displaying
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        const img = new Image();
        
        img.onload = function() {
            // Calculate new dimensions (max 800x800)
            let { width, height } = img;
            const maxDimension = 800;
            
            if (width > height) {
                if (width > maxDimension) {
                    height = (height * maxDimension) / width;
                    width = maxDimension;
                }
            } else {
                if (height > maxDimension) {
                    width = (width * maxDimension) / height;
                    height = maxDimension;
                }
            }
            
            canvas.width = width;
            canvas.height = height;
            
            // Draw and compress
            ctx.drawImage(img, 0, 0, width, height);
            const compressedDataUrl = canvas.toDataURL('image/jpeg', 0.7); // 70% quality
            
            imagePreview.src = compressedDataUrl;
            imagePreview.style.display = 'block';
        };
        
        img.src = URL.createObjectURL(file);
    } else {
        imagePreview.style.display = 'none';
        updateImageUploadText('Click to upload image');
    }
};

// Function to update image upload button text
function updateImageUploadText(text) {
    const uploadText = document.querySelector('.image-upload-text');
    if (uploadText) {
        uploadText.textContent = text;
    }
}

// Add drag and drop functionality
const imageUploadLabel = document.querySelector('.image-upload-label');
if (imageUploadLabel) {
    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        imageUploadLabel.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    // Highlight drop area when item is dragged over it
    ['dragenter', 'dragover'].forEach(eventName => {
        imageUploadLabel.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        imageUploadLabel.addEventListener(eventName, unhighlight, false);
    });

    // Handle dropped files
    imageUploadLabel.addEventListener('drop', handleDrop, false);

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    function highlight(e) {
        imageUploadLabel.style.background = '#e8f5e8';
        imageUploadLabel.style.borderColor = '#174b25';
    }

    function unhighlight(e) {
        imageUploadLabel.style.background = '#f8f9fa';
        imageUploadLabel.style.borderColor = '#206233';
    }

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        
        if (files.length > 0) {
            eventImage.files = files;
            // Trigger the change event
            const event = new Event('change', { bubbles: true });
            eventImage.dispatchEvent(event);
        }
    }
}

// Searchable dropdown for location with enhanced display
let filteredLocations = [];
let selectedLocationId = null;

eventLocation.oninput = function() {
    const val = eventLocation.value.toLowerCase();
    filteredLocations = locations.filter(loc => 
        loc.name.toLowerCase().includes(val) || 
        loc.building.toLowerCase().includes(val) ||
        loc.floor.toLowerCase().includes(val)
    );
    
    if (filteredLocations.length > 0 && val) {
        locationDropdown.innerHTML = filteredLocations.map(loc => 
            `<li data-location-id="${loc.id}" data-location-name="${loc.name}" data-location-building="${loc.building}" data-location-floor="${loc.floor}">
                <div class="location-name">${loc.name}</div>
                <div class="location-details">${loc.building} • ${loc.floor}</div>
            </li>`
        ).join('');
        locationDropdown.style.display = 'block';
    } else {
        locationDropdown.style.display = 'none';
    }
};

locationDropdown.onclick = function(e) {
    if (e.target.tagName === 'LI' || e.target.closest('li')) {
        const li = e.target.tagName === 'LI' ? e.target : e.target.closest('li');
        const locationName = li.dataset.locationName;
        const locationId = li.dataset.locationId;
        const building = li.dataset.locationBuilding;
        const floor = li.dataset.locationFloor;
        
        eventLocation.value = locationName;
        selectedLocationId = locationId;
        
        // Store additional location info for form submission
        eventLocation.dataset.locationId = locationId;
        eventLocation.dataset.locationBuilding = building;
        eventLocation.dataset.locationFloor = floor;
        
        locationDropdown.style.display = 'none';
    }
};

document.addEventListener('click', function(e) {
    if (!eventLocation.contains(e.target) && !locationDropdown.contains(e.target)) {
        locationDropdown.style.display = 'none';
    }
});

// Add or edit event submit
eventForm.onsubmit = async function(e) {
    e.preventDefault();
    
    // Check permissions before allowing submit
    if (!checkEventsPermission()) return;
    
    const name = eventName.value.trim();
    const description = eventDescription.value.trim();
    const location = eventLocation.value.trim();
    const startTime = eventStartTime.value;
    const endTime = eventEndTime.value;
    let image = '';
    
    // Get location details
    const locationId = eventLocation.dataset.locationId;
    const locationBuilding = eventLocation.dataset.locationBuilding;
    const locationFloor = eventLocation.dataset.locationFloor;
    
    // Find the full location object for coordinates
    const locationObj = locations.find(loc => loc.id === locationId);
    const locationCoordinates = locationObj ? locationObj.coordinates : { x: 0, y: 0, z: 0 };
    
    if (eventImage.files[0]) {
        // Use the compressed image from preview
        image = imagePreview.src;
        
        // Check if the compressed image is still too large
        if (image.length > 500000) { // Check if base64 string is over ~500KB
            alert('Image is still too large after compression. Please try a smaller image file.');
            return;
        }
    } else if (editingEventId) {
        image = editingEventImage;
    }
    
    // Validate form using centralized validation
    const form = document.getElementById('eventForm');
    const validationRules = {
        eventName: { required: true, minLength: 2, maxLength: 100 },
        eventDescription: { required: true, minLength: 10, maxLength: 500 },
        eventLocation: { 
            required: true, 
            minLength: 2, 
            maxLength: 200,
            custom: (value, form) => {
                // Check if location was selected from dropdown
                if (!locationId || !locationObj) {
                    return 'Please select a valid location from the dropdown';
                }
                return true;
            }
        },
        eventStartTime: { required: true, time: true },
        eventEndTime: { 
            required: true, 
            time: true,
            custom: (value, form) => {
                const startTime = form.querySelector('[name="eventStartTime"]').value;
                if (startTime && new Date(value) <= new Date(startTime)) {
                    return 'End time must be after start time';
                }
                return true;
            }
        }
    };
    
    const validation = window.validationUtils.validateForm(form, validationRules);
    if (!validation.isValid) {
        window.validationUtils.showFormErrors(form, validation.errors);
        return;
    }
    
    // Check time conflict before confirmation
    const conflict = await hasTimeConflict(location, new Date(startTime).toISOString(), new Date(endTime).toISOString(), editingEventId);
    if (conflict) {
        const conflictStart = new Date(conflict.start).toLocaleString();
        const conflictEnd = new Date(conflict.end).toLocaleString();
        alert(`This location is already booked between ${conflictStart} and ${conflictEnd} for "${conflict.name}". Please choose a different time or location.`);
        return;
    }

    // Ask for confirmation before adding/updating
    let confirmMessage = '';
    if (editingEventId) {
        confirmMessage = `Do you want to update the event "${name}"?`;
    } else {
        confirmMessage = `Do you want to add the event "${name}"?`;
    }
    
    const confirmed = confirm(confirmMessage);
    
    if (confirmed) {
        try {
            // Prepare event data
            const eventData = {
                name,
                description,
                location,
                locationId,
                locationBuilding,
                locationFloor,
                locationCoordinates,
                startTime: new Date(startTime).toISOString(),
                endTime: new Date(endTime).toISOString(),
                image
            };
            
            // User clicked "OK" - proceed with adding/updating
            if (editingEventId) {
                // Update existing event
                console.log('Updating event:', editingEventId, eventData);
                await updateDoc(doc(db, "events", editingEventId), eventData);
                console.log('Event updated successfully');
            } else {
                // Add new event
                console.log('Adding new event:', eventData);
                await addDoc(eventsRef, eventData);
                console.log('Event added successfully');
            }
            
            // Refresh the events list
            await fetchAndRenderEvents();
            
            // Close modal and reset form
            addEventModal.style.display = 'none';
            const wasEditing = !!editingEventId;
            editingEventId = null;
            editingEventImage = '';
            eventForm.reset();
            imagePreview.style.display = 'none';
            
            // Clear location metadata
            delete eventLocation.dataset.locationId;
            delete eventLocation.dataset.locationBuilding;
            delete eventLocation.dataset.locationFloor;
            selectedLocationId = null;
            
            // Show success message
            alert(wasEditing ? 'Event updated successfully!' : 'Event added successfully!');
            
        } catch (error) {
            console.error('Error saving event:', error);
            alert('Error saving event: ' + error.message);
        }
    } else {
        console.log('User cancelled the operation');
    }
};

// Archive modal functionality
let selectedArchivedEvents = new Set();
let allArchiveItems = [];
let filteredArchiveItems = [];

async function openArchiveModal() {
    const archiveModal = document.getElementById('archiveModal');
    const archiveList = document.getElementById('archiveList');
    
    if (!archiveModal || !archiveList) return;
    
    try {
        // Show loading state
        archiveList.innerHTML = '<div style="text-align:center; padding:40px; color:#666;">Loading archived events...</div>';
        archiveModal.style.display = 'flex';
        
        // Fetch archived events from events_archive collection
        const archiveRef = collection(db, "events_archive");
        const querySnapshot = await getDocs(archiveRef);
        allArchiveItems = [];
        
        querySnapshot.forEach((docSnap) => {
            const data = docSnap.data();
            const event = {
                id: docSnap.id,
                name: data.name || data.title || 'Untitled Event',
                description: data.description || '',
                location: data.location || '',
                locationBuilding: data.locationBuilding || '',
                locationFloor: data.locationFloor || '',
                startTime: data.startTime || '',
                endTime: data.endTime || '',
                image: data.image || '',
                archivedAt: data.archivedAt ? new Date(data.archivedAt) : null,
                archivedAtString: data.archivedAt ? new Date(data.archivedAt).toLocaleString() : 'Unknown',
                archiveReason: data.archiveReason || 'expired' // 'expired', 'deleted', or 'manual'
            };
            allArchiveItems.push(event);
        });
        
        // Sort by archive time (most recent first)
        allArchiveItems.sort((a, b) => {
            const aTime = a.archivedAt ? a.archivedAt : new Date(0);
            const bTime = b.archivedAt ? b.archivedAt : new Date(0);
            return bTime - aTime;
        });
        
        filteredArchiveItems = [...allArchiveItems];
        renderArchiveItems();
        setupSearchAndFilter();
        
        // Clear selection
        selectedArchivedEvents.clear();
        updateArchiveModalButtons();
        
    } catch (error) {
        console.error('Error loading archived events:', error);
        archiveList.innerHTML = '<div style="text-align:center; padding:40px; color:#d32f2f;">Error loading archived events. Please try again.</div>';
    }
}

function renderArchiveItems() {
    const archiveList = document.getElementById('archiveList');
    if (!archiveList) return;
    
    if (filteredArchiveItems.length === 0) {
        archiveList.innerHTML = '<div style="text-align:center; padding:40px; color:#666; font-style:italic;">No archived events found.</div>';
        return;
    }
    
    archiveList.innerHTML = filteredArchiveItems.map(event => {
        const locationDisplay = event.locationBuilding && event.locationFloor 
            ? `${event.location} (${event.locationBuilding} • ${event.locationFloor})`
            : event.location;
        
        // Determine archive reason display
        let archiveReasonDisplay = '';
        if (event.archiveReason === 'deleted') {
            archiveReasonDisplay = '🗑️ Manually deleted';
        } else if (event.archiveReason === 'expired') {
            archiveReasonDisplay = '⏰ Expired';
        } else {
            archiveReasonDisplay = '📁 Archived';
        }
        
        return `
            <div class="archived-event-item" data-event-id="${event.id}">
                <div class="archived-event-checkbox">
                    <input type="checkbox" class="archive-event-checkbox" data-event-id="${event.id}">
                </div>
                <div class="archived-event-content">
                    <div class="archived-event-image">
                        ${event.image ? `<img src="${event.image}" style="width:50px;height:50px;object-fit:cover;border-radius:8px;"/>` : '<div class="no-image-placeholder">📅</div>'}
                    </div>
                    <div class="archived-event-details">
                        <div class="archived-event-title">${event.name}</div>
                        <div class="archived-event-location">📍 ${locationDisplay}</div>
                        <div class="archived-event-time">📅 ${event.startTime ? new Date(event.startTime).toLocaleString() : 'No time set'}</div>
                        <div class="archived-event-date">${archiveReasonDisplay}: ${event.archivedAtString}</div>
                    </div>
                    <div class="archived-event-actions">
                        <button class="mini-btn success" onclick="restoreSingleEvent('${event.id}')">Restore</button>
                        <button class="mini-btn danger" onclick="deleteSingleArchivedEvent('${event.id}')">Delete</button>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

function setupSearchAndFilter() {
    const searchInput = document.getElementById('archiveSearchInput');
    const filterDropdownBtn = document.getElementById('filterDropdownBtn');
    const filterDropdownMenu = document.getElementById('filterDropdownMenu');
    const filterDropdownText = document.getElementById('filterDropdownText');

    // Search functionality
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            const searchTerm = e.target.value.toLowerCase().trim();
            filterArchiveItems(searchTerm);
        });
    }

    // Dropdown toggle functionality
    if (filterDropdownBtn && filterDropdownMenu) {
        filterDropdownBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            const isOpen = filterDropdownMenu.style.display === 'block';
            filterDropdownMenu.style.display = isOpen ? 'none' : 'block';
            filterDropdownBtn.classList.toggle('open', !isOpen);
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', (e) => {
            if (!filterDropdownBtn.contains(e.target) && !filterDropdownMenu.contains(e.target)) {
                filterDropdownMenu.style.display = 'none';
                filterDropdownBtn.classList.remove('open');
            }
        });

        // Filter option clicks
        const filterOptions = filterDropdownMenu.querySelectorAll('.filter-option');
        filterOptions.forEach(option => {
            option.addEventListener('click', () => {
                const filterType = option.dataset.filter;
                const filterText = option.textContent.trim();
                
                // Update button text
                filterDropdownText.textContent = filterText;
                
                // Update active state
                filterOptions.forEach(opt => opt.classList.remove('active'));
                option.classList.add('active');
                
                // Close dropdown
                filterDropdownMenu.style.display = 'none';
                filterDropdownBtn.classList.remove('open');
                
                // Apply filter
                setActiveFilter(filterType);
                filterArchiveItems(document.getElementById('archiveSearchInput')?.value || '');
            });
        });
    }
}

function setActiveFilter(filterType) {
    // Update the dropdown button text and active state
    const filterDropdownText = document.getElementById('filterDropdownText');
    const filterOptions = document.querySelectorAll('.filter-option');
    
    // Remove active class from all options
    filterOptions.forEach(opt => opt.classList.remove('active'));
    
    // Add active class to selected option
    const selectedOption = document.querySelector(`[data-filter="${filterType}"]`);
    if (selectedOption) {
        selectedOption.classList.add('active');
    }
}

function filterArchiveItems(searchTerm = '') {
    const activeFilter = document.querySelector('.filter-option.active')?.dataset.filter;
    const now = new Date();
    const thirtyDaysAgo = new Date(now.getTime() - (30 * 24 * 60 * 60 * 1000));

    filteredArchiveItems = allArchiveItems.filter(item => {
        // Search filter
        const matchesSearch = !searchTerm || 
            item.name.toLowerCase().includes(searchTerm) ||
            item.description.toLowerCase().includes(searchTerm) ||
            item.location.toLowerCase().includes(searchTerm) ||
            item.locationBuilding.toLowerCase().includes(searchTerm) ||
            item.locationFloor.toLowerCase().includes(searchTerm);

        // Time filter
        let matchesTime = true;
        if (activeFilter === 'recent') {
            matchesTime = item.archivedAt && item.archivedAt >= thirtyDaysAgo;
        } else if (activeFilter === 'old') {
            matchesTime = item.archivedAt && item.archivedAt < thirtyDaysAgo;
        }

        return matchesSearch && matchesTime;
    });

    renderArchiveItems();
}

function closeArchiveModal() {
    const archiveModal = document.getElementById('archiveModal');
    if (archiveModal) {
        archiveModal.style.display = 'none';
        selectedArchivedEvents.clear();
    }
}

function updateArchiveModalButtons() {
    const selectAllBtn = document.getElementById('selectAllArchiveBtn');
    const deleteSelectedBtn = document.getElementById('deleteSelectedArchiveBtn');
    const restoreSelectedBtn = document.getElementById('restoreSelectedArchiveBtn');
    const clearAllBtn = document.getElementById('clearAllArchiveBtn');
    
    if (selectAllBtn) {
        const allCheckboxes = document.querySelectorAll('.archive-event-checkbox');
        const checkedCount = document.querySelectorAll('.archive-event-checkbox:checked').length;
        
        if (checkedCount === 0) {
            selectAllBtn.textContent = 'Select All';
        } else if (checkedCount === allCheckboxes.length) {
            selectAllBtn.textContent = 'Deselect All';
        } else {
            selectAllBtn.textContent = `Select All (${checkedCount}/${allCheckboxes.length})`;
        }
    }
    
    const hasSelection = selectedArchivedEvents.size > 0;
    const hasItems = filteredArchiveItems.length > 0;
    
    if (deleteSelectedBtn) deleteSelectedBtn.disabled = !hasSelection;
    if (restoreSelectedBtn) restoreSelectedBtn.disabled = !hasSelection;
    if (clearAllBtn) clearAllBtn.disabled = !hasItems;
}

async function restoreSingleEvent(eventId) {
    console.log('Attempting to restore event:', eventId);
    if (!confirm('Are you sure you want to restore this event?')) return;
    
    try {
        console.log('Getting archived event from events_archive collection:', eventId);
        
        // Get the archived event data
        const archivedEventRef = doc(db, "events_archive", eventId);
        const archivedEventSnap = await getDoc(archivedEventRef);
        
        if (!archivedEventSnap.exists()) {
            alert('Archived event not found!');
            return;
        }
        
        const archivedData = archivedEventSnap.data();
        
        // Remove archivedAt field and add restoredAt
        const { archivedAt, ...eventData } = archivedData;
        eventData.restoredAt = new Date().toISOString();
        
        console.log('Adding event back to events collection');
        
        // Add the event back to the main events collection
        await addDoc(eventsRef, eventData);
        
        console.log('Deleting from events_archive collection');
        
        // Delete from archive collection
        await deleteDoc(archivedEventRef);
        
        console.log('Successfully restored event, updating UI');
        
        // Remove from arrays
        allArchiveItems = allArchiveItems.filter(item => item.id !== eventId);
        filteredArchiveItems = filteredArchiveItems.filter(item => item.id !== eventId);
        selectedArchivedEvents.delete(eventId);
        
        // Re-render the list
        renderArchiveItems();
        updateArchiveModalButtons();
        
        // Refresh main events list
        await fetchAndRenderEvents();
        alert('Event restored successfully!');
        
    } catch (error) {
        console.error('Error restoring event:', error);
        alert('Error restoring event: ' + error.message);
    }
}

async function deleteSingleArchivedEvent(eventId) {
    if (!confirm('Are you sure you want to permanently delete this archived event? This action cannot be undone.')) return;
    
    try {
        console.log('Deleting archived event from events_archive collection:', eventId);
        
        // Delete from events_archive collection
        await deleteDoc(doc(db, "events_archive", eventId));
        
        console.log('Successfully deleted archived event');
        
        // Remove from arrays
        allArchiveItems = allArchiveItems.filter(item => item.id !== eventId);
        filteredArchiveItems = filteredArchiveItems.filter(item => item.id !== eventId);
        selectedArchivedEvents.delete(eventId);
        
        // Re-render the list
        renderArchiveItems();
        updateArchiveModalButtons();
        
        alert('Event deleted permanently!');
        
    } catch (error) {
        console.error('Error deleting archived event:', error);
        alert('Error deleting event: ' + error.message);
    }
}

// Bulk action functions
function selectAllArchiveItems() {
    const allCheckboxes = document.querySelectorAll('.archive-event-checkbox');
    const allChecked = Array.from(allCheckboxes).every(cb => cb.checked);
    
    allCheckboxes.forEach(cb => {
        cb.checked = !allChecked;
        const eventId = cb.dataset.eventId;
        if (cb.checked) {
            selectedArchivedEvents.add(eventId);
        } else {
            selectedArchivedEvents.delete(eventId);
        }
    });
    
    updateArchiveModalButtons();
}

async function restoreSelectedArchiveItems() {
    if (selectedArchivedEvents.size === 0) return;
    
    const count = selectedArchivedEvents.size;
    console.log('Attempting to restore', count, 'events:', Array.from(selectedArchivedEvents));
    if (!confirm(`Are you sure you want to restore ${count} archived event(s)?`)) return;
    
    try {
        console.log('Starting bulk restore operation');
        
        const restorePromises = Array.from(selectedArchivedEvents).map(async (eventId) => {
            console.log('Restoring event:', eventId);
            
            // Get the archived event data
            const archivedEventRef = doc(db, "events_archive", eventId);
            const archivedEventSnap = await getDoc(archivedEventRef);
            
            if (!archivedEventSnap.exists()) {
                console.warn('Archived event not found:', eventId);
                return;
            }
            
            const archivedData = archivedEventSnap.data();
            
            // Remove archivedAt field and add restoredAt
            const { archivedAt, ...eventData } = archivedData;
            eventData.restoredAt = new Date().toISOString();
            
            // Add the event back to the main events collection
            await addDoc(eventsRef, eventData);
            
            // Delete from archive collection
            await deleteDoc(archivedEventRef);
            
            console.log('Successfully restored event:', eventId);
        });
        
        await Promise.all(restorePromises);
        console.log('All events restored successfully');
        
        // Remove from arrays
        allArchiveItems = allArchiveItems.filter(item => !selectedArchivedEvents.has(item.id));
        filteredArchiveItems = filteredArchiveItems.filter(item => !selectedArchivedEvents.has(item.id));
        
        selectedArchivedEvents.clear();
        renderArchiveItems();
        updateArchiveModalButtons();
        
        // Refresh main events list
        await fetchAndRenderEvents();
        alert(`${count} event(s) restored successfully!`);
        
    } catch (error) {
        console.error('Error restoring selected events:', error);
        alert('Error restoring events: ' + error.message);
    }
}

async function deleteSelectedArchiveItems() {
    if (selectedArchivedEvents.size === 0) return;
    
    const count = selectedArchivedEvents.size;
    if (!confirm(`Are you sure you want to permanently delete ${count} archived event(s)? This action cannot be undone.`)) return;
    
    try {
        console.log('Starting bulk delete operation for', count, 'events');
        
        const deletePromises = Array.from(selectedArchivedEvents).map(eventId => {
            console.log('Deleting archived event:', eventId);
            return deleteDoc(doc(db, "events_archive", eventId));
        });
        
        await Promise.all(deletePromises);
        console.log('All events deleted successfully');
        
        // Remove from arrays
        allArchiveItems = allArchiveItems.filter(item => !selectedArchivedEvents.has(item.id));
        filteredArchiveItems = filteredArchiveItems.filter(item => !selectedArchivedEvents.has(item.id));
        
        selectedArchivedEvents.clear();
        renderArchiveItems();
        updateArchiveModalButtons();
        
        alert(`${count} event(s) deleted permanently!`);
        
    } catch (error) {
        console.error('Error deleting selected events:', error);
        alert('Error deleting events: ' + error.message);
    }
}

async function clearAllArchiveItems() {
    const activeFilter = document.querySelector('.filter-option.active')?.dataset.filter;
    let confirmMessage = 'Delete ALL archived events? This action cannot be undone.';
    
    if (activeFilter === 'recent') {
        confirmMessage = 'Delete ALL recent archived events (last 30 days)? This action cannot be undone.';
    } else if (activeFilter === 'old') {
        confirmMessage = 'Delete ALL old archived events (older than 30 days)? This action cannot be undone.';
    }
    
    if (!confirm(confirmMessage)) return;
    
    try {
        console.log('Starting clear all operation');
        
        // Delete only filtered items
        const itemsToDelete = filteredArchiveItems.map(item => item.id);
        
        const deletePromises = itemsToDelete.map(id => {
            console.log('Deleting archived event:', id);
            return deleteDoc(doc(db, "events_archive", id));
        });
        
        await Promise.all(deletePromises);
        console.log('All filtered events deleted successfully');
        
        // Remove from arrays
        allArchiveItems = allArchiveItems.filter(item => !itemsToDelete.includes(item.id));
        filteredArchiveItems = [];
        selectedArchivedEvents.clear();
        
        renderArchiveItems();
        updateArchiveModalButtons();
        
        alert(`${itemsToDelete.length} event(s) deleted permanently!`);
        
    } catch (error) {
        console.error('Error clearing archive items:', error);
        alert('Error clearing events: ' + error.message);
    }
}

// Initial fetch
document.addEventListener('DOMContentLoaded', async function() {
    // Fetch locations first, then events
    await fetchLocations();
    await fetchAndRenderEvents();
    
    const refreshBtn = document.getElementById('refreshEventsBtn');
    if (refreshBtn) {
        refreshBtn.onclick = async function() {
            refreshBtn.classList.add('loading');
            refreshBtn.setAttribute('aria-busy', 'true');
            refreshBtn.disabled = true;
            try {
                await fetchLocations();
                await fetchAndRenderEvents(false);
            } finally {
                refreshBtn.disabled = false;
                refreshBtn.removeAttribute('aria-busy');
                refreshBtn.classList.remove('loading');
            }
        };
    }
    
    // Add archive modal functionality
    const archiveToggleBtn = document.getElementById('archiveToggleBtn');
    if (archiveToggleBtn) {
        archiveToggleBtn.onclick = openArchiveModal;
    }
    
    // Archive modal event listeners
    const closeArchiveBtn = document.getElementById('closeArchiveBtn');
    if (closeArchiveBtn) {
        closeArchiveBtn.onclick = closeArchiveModal;
    }
    
    const selectAllArchiveBtn = document.getElementById('selectAllArchiveBtn');
    if (selectAllArchiveBtn) {
        selectAllArchiveBtn.onclick = selectAllArchiveItems;
    }
    
    const deleteSelectedArchiveBtn = document.getElementById('deleteSelectedArchiveBtn');
    if (deleteSelectedArchiveBtn) {
        deleteSelectedArchiveBtn.onclick = deleteSelectedArchiveItems;
    }
    
    const restoreSelectedArchiveBtn = document.getElementById('restoreSelectedArchiveBtn');
    if (restoreSelectedArchiveBtn) {
        restoreSelectedArchiveBtn.onclick = restoreSelectedArchiveItems;
    }
    
    const clearAllArchiveBtn = document.getElementById('clearAllArchiveBtn');
    if (clearAllArchiveBtn) {
        clearAllArchiveBtn.onclick = clearAllArchiveItems;
    }
    
    // Handle checkbox changes in archive modal
    document.addEventListener('change', function(e) {
        if (e.target.classList.contains('archive-event-checkbox')) {
            const eventId = e.target.dataset.eventId;
            if (e.target.checked) {
                selectedArchivedEvents.add(eventId);
            } else {
                selectedArchivedEvents.delete(eventId);
            }
            updateArchiveModalButtons();
        }
    });
    
    // Make functions globally accessible for onclick handlers
    window.restoreSingleEvent = restoreSingleEvent;
    window.deleteSingleArchivedEvent = deleteSingleArchivedEvent;
});
