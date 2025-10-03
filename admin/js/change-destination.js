import { initializeApp } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-app.js";
import { getFirestore, collection, getDocs, doc, getDoc, updateDoc, deleteDoc, query, where, orderBy } from "https://www.gstatic.com/firebasejs/12.0.0/firebase-firestore.js";

// Firebase config (same as events.js)
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
const coordinatesRef = collection(db, "coordinates");
const buildingsRef = collection(db, "buildings");
const floorsRef = collection(db, "floors");

// DOM elements
const loadingState = document.getElementById('loadingState');
const container = document.getElementById('destinationsContainer');
const destinationList = document.getElementById('destinationList');
const searchInput = document.getElementById('searchInput');

// View modal elements
const viewDestinationModal = document.getElementById('viewDestinationModal');
const closeViewModalBtn = document.getElementById('closeViewModalBtn');
const editDestBtn = document.getElementById('editDestBtn');
const deleteDestBtn = document.getElementById('deleteDestBtn');
const viewDestMedia = document.getElementById('viewDestMedia');
const viewDestName = document.getElementById('viewDestName');
const viewDestBuilding = document.getElementById('viewDestBuilding');
const viewDestFloor = document.getElementById('viewDestFloor');
const viewDestCoords = document.getElementById('viewDestCoords');
const viewDestDescription = document.getElementById('viewDestDescription');

// Edit modal elements
const editDestinationModal = document.getElementById('editDestinationModal');
const closeEditModalBtn = document.getElementById('closeEditModalBtn');
const destForm = document.getElementById('destForm');
const destName = document.getElementById('destName');
const destBuilding = document.getElementById('destBuilding');
const destFloor = document.getElementById('destFloor');
const destX = document.getElementById('destX');
const destY = document.getElementById('destY');
const destZ = document.getElementById('destZ');
const destImage = document.getElementById('destImage');
const destDescription = document.getElementById('destDescription');

let allDestinations = [];
let filteredDestinations = [];
let viewingId = null;

// Caches for building/floor metadata
const buildingIdToName = new Map();
const floorIdToMeta = new Map(); // id -> { number, name, buildingId }

function showLoading() {
  if (loadingState) loadingState.style.display = '';
  if (container) container.style.display = 'none';
}

function hideLoading() {
  if (loadingState) loadingState.style.display = 'none';
  if (container) container.style.display = '';
}

function safeText(value, fallback = '—') {
  if (value === undefined || value === null) return fallback;
  const str = String(value).trim();
  return str.length ? str : fallback;
}

function normalizeCoordinateData(id, data) {
  // Try to normalize various potential field shapes
  const name = data.name || data.title || data.label || `Location ${id}`;
  const buildingId = data.buildingId || '';
  const floorId = data.floorId || '';
  const legacyBuilding = data.building || data.block || '';
  const legacyFloor = data.floor !== undefined ? data.floor : (data.level !== undefined ? data.level : '');
  const x = data.x ?? (data.coords && data.coords.x);
  const y = data.y ?? (data.coords && data.coords.y);
  const z = data.z ?? (data.coords && data.coords.z);
  const image = data.image || data.thumbnail || data.photoUrl || '';
  const description = data.description || data.details || '';

  // Resolve human-friendly fields
  const buildingName = buildingId ? (buildingIdToName.get(buildingId) || '') : (legacyBuilding || '');
  let floorDisplay = '';
  if (floorId) {
    const meta = floorIdToMeta.get(floorId);
    if (meta) floorDisplay = (meta.number !== undefined && meta.number !== null && !Number.isNaN(meta.number)) ? meta.number : (meta.name || '');
  } else if (legacyFloor !== '' && legacyFloor !== null && legacyFloor !== undefined) {
    floorDisplay = legacyFloor;
  }

  return { id, name, buildingId, floorId, buildingName, floorDisplay, x, y, z, image, description };
}

async function preloadBuildingsAndFloors() {
  buildingIdToName.clear();
  floorIdToMeta.clear();
  const [buildingsSnap, floorsSnap] = await Promise.all([
    getDocs(query(buildingsRef, orderBy('name'))),
    getDocs(floorsRef)
  ]);
  buildingsSnap.forEach(d => {
    const data = d.data() || {};
    buildingIdToName.set(d.id, String(data.name || ''));
  });
  floorsSnap.forEach(d => {
    const data = d.data() || {};
    floorIdToMeta.set(d.id, {
      number: typeof data.number === 'number' ? data.number : parseInt(String(data.number ?? ''), 10),
      name: String(data.name || ''),
      buildingId: String(data.buildingId || '')
    });
  });
}

async function fetchDestinations() {
  // Ensure metadata is loaded so names are resolved
  if (buildingIdToName.size === 0 || floorIdToMeta.size === 0) {
    await preloadBuildingsAndFloors();
  }
  const docs = await getDocs(coordinatesRef);
  const items = [];
  docs.forEach(d => {
    const normalized = normalizeCoordinateData(d.id, d.data());
    items.push(normalized);
  });
  allDestinations = items.sort((a, b) => a.name.localeCompare(b.name));
  filteredDestinations = allDestinations;
}

function renderDestinations(list) {
  if (!destinationList) return;
  if (!list || list.length === 0) {
    destinationList.innerHTML = '<p class="no-events">No destinations found.</p>';
    return;
  }

  destinationList.innerHTML = '';
  list.forEach(item => {
    const el = document.createElement('div');
    el.className = 'destination-item';
    el.setAttribute('data-id', item.id);
    el.style.cursor = 'pointer';
    el.innerHTML = `
      <div class="dest-thumb">
        ${item.image ? `<img class="destination-img" src="${item.image}" alt="${item.name}">` : `
          <div class="dest-img-placeholder">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none">
              <rect x="2" y="2" width="20" height="20" rx="4" fill="#f0f0f0"/>
              <text x="12" y="16" text-anchor="middle" font-size="16" fill="#bbb">?</text>
            </svg>
          </div>
        `}
      </div>
      <div class="destination-info">
        <span class="destination-name">${safeText(item.name, 'Unnamed')}</span>
        <span class="destination-distance">
          ${safeText(item.buildingName, '')}
        </span>
      </div>
    `;
    destinationList.appendChild(el);
  });
}

async function openViewModal(id) {
  try {
    // Fetch latest doc to ensure fresh data
    const snap = await getDoc(doc(db, 'coordinates', id));
    if (!snap.exists()) return;
    const item = normalizeCoordinateData(id, snap.data());

    viewingId = id;
    viewDestName.textContent = safeText(item.name, 'Unnamed');
    viewDestBuilding.textContent = safeText(item.buildingName, '—');
    viewDestFloor.textContent = (item.floorDisplay === '' || item.floorDisplay === null || item.floorDisplay === undefined) ? '—' : String(item.floorDisplay);
    const hasXYZ = [item.x, item.y, item.z].some(v => v !== undefined && v !== null);
    viewDestCoords.textContent = hasXYZ ? `${item.x ?? '—'}, ${item.y ?? '—'}, ${item.z ?? '—'}` : '—';
    viewDestDescription.textContent = safeText(item.description, 'No description');

    // Render image or placeholder like events list style
    if (viewDestMedia) {
      if (item.image) {
        viewDestMedia.innerHTML = `<img src="${item.image}" style="max-width:200px; max-height:200px; object-fit:cover; border-radius:8px;"/>`;
      } else {
        viewDestMedia.innerHTML = `
          <svg width="120" height="120" viewBox="0 0 24 24" fill="none">
            <rect x="2" y="2" width="20" height="20" rx="4" fill="#f0f0f0"/>
            <text x="12" y="16" text-anchor="middle" font-size="18" fill="#bbb">?</text>
          </svg>
        `;
      }
    }

    viewDestinationModal.style.display = 'flex';
  } catch (err) {
    console.error('Failed to open destination view:', err);
    alert('Failed to load destination details.');
  }
}

if (closeViewModalBtn) {
  closeViewModalBtn.onclick = function() {
    viewDestinationModal.style.display = 'none';
    viewingId = null;
  };
}

if (editDestBtn) {
  editDestBtn.onclick = async function() {
    if (!viewingId) return;
    try {
      const snap = await getDoc(doc(db, 'coordinates', viewingId));
      if (!snap.exists()) return;
      const item = normalizeCoordinateData(viewingId, snap.data());

      // Pre-fill form
      destName.value = item.name || '';
      // Building select expects an ID when using dropdown
      destBuilding.value = item.buildingId || '';
      // Reload floors for the selected building and then set the floor
      if (destBuilding.value) {
        const buildingId = destBuilding.value;
        // Load floors options similar to inline script
        if (typeof window.setEditModalBuildingAndFloor === 'function') {
          await window.setEditModalBuildingAndFloor(buildingId, item.floorId || '');
        } else {
          destFloor.value = item.floorId || '';
        }
      } else {
        destFloor.value = item.floorId || '';
      }
      destX.value = item.x !== undefined && item.x !== null ? item.x : '';
      destY.value = item.y !== undefined && item.y !== null ? item.y : '';
      destZ.value = item.z !== undefined && item.z !== null ? item.z : '';
      destImage.value = item.image || '';
      destDescription.value = item.description || '';

      // Switch modals
      viewDestinationModal.style.display = 'none';
      editDestinationModal.style.display = 'flex';
    } catch (e) {
      console.error('Failed to open edit modal:', e);
      alert('Failed to open edit form.');
    }
  };
}

if (closeEditModalBtn) {
  closeEditModalBtn.onclick = function() {
    editDestinationModal.style.display = 'none';
    viewingId = null;
  };
}

if (deleteDestBtn) {
  deleteDestBtn.onclick = async function() {
    if (!viewingId) return;
    const confirmed = confirm('Are you sure you want to delete this destination?');
    if (!confirmed) return;
    try {
      await deleteDoc(doc(db, 'coordinates', viewingId));
      viewDestinationModal.style.display = 'none';
      viewingId = null;
      await fetchDestinations();
      renderDestinations(filteredDestinations);
      alert('Destination deleted successfully');
    } catch (e) {
      console.error('Failed to delete destination:', e);
      alert('Failed to delete destination.');
    }
  };
}

if (destForm) {
  destForm.onsubmit = async function(e) {
    e.preventDefault();
    if (!viewingId) return;

    const payload = {
      name: destName.value.trim(),
      description: destDescription.value.trim() || ''
    };

    // Save by IDs primarily (aligning with add-location.html)
    const bId = (destBuilding.value || '').trim();
    const fId = (destFloor.value || '').trim();
    payload.buildingId = bId || null;
    payload.floorId = fId || null;
    // Also maintain legacy fields for backward compatibility in UI (optional)
    if (bId) {
      payload.building = buildingIdToName.get(bId) || '';
    } else {
      payload.building = '';
    }

    const floorMeta = fId ? floorIdToMeta.get(fId) : null;
    if (floorMeta && floorMeta.number !== undefined && floorMeta.number !== null && !Number.isNaN(floorMeta.number)) {
      payload.floor = floorMeta.number;
    } else if (destFloor.value !== '') {
      payload.floor = destFloor.value; // keep whatever is selected/displayed
    } else {
      payload.floor = '';
    }

    const xVal = destX.value;
    const yVal = destY.value;
    const zVal = destZ.value;
    if (xVal !== '') payload.x = Number(xVal);
    if (yVal !== '') payload.y = Number(yVal);
    if (zVal !== '') payload.z = Number(zVal);

    const imageVal = destImage.value.trim();
    if (imageVal) payload.image = imageVal; else payload.image = '';

    if (!payload.name) {
      alert('Name is required');
      return;
    }

    try {
      await updateDoc(doc(db, 'coordinates', viewingId), payload);
      editDestinationModal.style.display = 'none';
      viewingId = null;
      await fetchDestinations();
      renderDestinations(filteredDestinations);
      alert('Destination updated successfully');
    } catch (e) {
      console.error('Failed to update destination:', e);
      alert('Failed to save changes.');
    }
  };
}

function applySearchFilter(term) {
  const q = term.trim().toLowerCase();
  if (!q) {
    filteredDestinations = allDestinations;
  } else {
    filteredDestinations = allDestinations.filter(d => {
      return (
        (d.name && d.name.toLowerCase().includes(q)) ||
        (d.building && d.building.toLowerCase().includes(q)) ||
        (String(d.floor).toLowerCase().includes(q))
      );
    });
  }
  renderDestinations(filteredDestinations);
}

function debounce(fn, delay = 250) {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), delay);
  };
}

document.addEventListener('DOMContentLoaded', async function() {
  try {
    showLoading();
    // Ensure user is authenticated; rely on auth-check.js listener already included
    await fetchDestinations();
    renderDestinations(filteredDestinations);
  } catch (e) {
    console.error('Error loading destinations:', e);
    destinationList.innerHTML = '<p class="error-message">Error loading destinations. Please try again.</p>';
  } finally {
    hideLoading();
  }

  if (searchInput) {
    const onSearch = debounce(() => applySearchFilter(searchInput.value), 200);
    searchInput.addEventListener('input', onSearch);
  }

  // Event delegation for item clicks
  if (destinationList) {
    destinationList.addEventListener('click', async function(e) {
      const card = e.target.closest('.destination-item');
      if (!card) return;
      const id = card.getAttribute('data-id');
      await openViewModal(id);
    });
  }
});


