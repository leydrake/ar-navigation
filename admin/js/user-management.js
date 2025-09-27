// Minimal styles reliance on separate CSS; logic only
// Guard access: require users permission or super admin
document.addEventListener('DOMContentLoaded', () => {
    if (typeof requirePermission === 'function') {
        if (!requirePermission('users')) return;
    }
});

(function(){
    // Firebase compat (matching admin/index.html usage)
    const firebaseConfig = {
        apiKey: "AIzaSyB8Xi8J7t3wRSy1TeIxiGFz-Is6U0zDFVg",
        authDomain: "navigatecampus.firebaseapp.com",
        projectId: "navigatecampus",
        storageBucket: "navigatecampus.appspot.com",
        messagingSenderId: "55012323145",
        appId: "1:55012323145:web:3408681d5a450f05b2b498",
        measurementId: "G-39WFZN3VPV"
    };
    if (!firebase.apps.length) {
        firebase.initializeApp(firebaseConfig);
    }
    const db = firebase.firestore();

    const addUserForm = document.getElementById('addUserForm');
    const usersTableBody = document.getElementById('usersTableBody');

    function getSelectedPermissions(container=document) {
        const boxes = container.querySelectorAll('input[type="checkbox"][data-permission]');
        const permissions = [];
        boxes.forEach(b => { if (b.checked) permissions.push(b.getAttribute('data-permission')); });
        return permissions;
    }

    function renderUserRow(id, data) {
        const tr = document.createElement('tr');
        const perms = Array.isArray(data.permissions) ? data.permissions : [];
        
        // Get current user's role
        const currentUserRole = sessionStorage.getItem('adminRole');
        const targetUserRole = data.role || 'admin';
        
        // Check if current user is super admin and target user is also super admin
        const isCurrentUserSuperAdmin = currentUserRole === 'super_admin';
        const isTargetUserSuperAdmin = targetUserRole === 'super_admin';
        const isSameUser = sessionStorage.getItem('adminEmail') === data.email;
        
        // Super admins cannot edit other super admins (except themselves)
        const canEdit = !(isCurrentUserSuperAdmin && isTargetUserSuperAdmin && !isSameUser);
        
        let actionButtons = '';
        if (canEdit) {
            actionButtons = `
                <button class="sm-btn" data-action="edit" data-id="${id}">Edit</button>
                <button class="sm-btn danger" data-action="toggle" data-id="${id}">${data.disabled ? 'Enable' : 'Disable'}</button>
                <button class="sm-btn danger" data-action="delete" data-id="${id}">Delete</button>
            `;
        } else {
            actionButtons = `
                <span class="no-actions-text">Protected</span>
            `;
        }
        
        tr.innerHTML = `
            <td>${data.name || ''}</td>
            <td>${data.email}</td>
            <td>${data.role || 'admin'}</td>
            <td>${perms.join(', ')}</td>
            <td>${data.disabled ? 'Disabled' : 'Active'}</td>
            <td>${actionButtons}</td>
        `;
        return tr;
    }

    async function loadUsers() {
        usersTableBody.innerHTML = '';
        const snap = await db.collection('admin_credentials').get();
        snap.forEach(doc => {
            const row = renderUserRow(doc.id, doc.data());
            usersTableBody.appendChild(row);
        });
    }

    async function createUser(e) {
        e.preventDefault();
        const email = document.getElementById('userEmail').value.trim();
        const password = document.getElementById('userPassword').value;
        const role = document.getElementById('userRole').value;
        const name = document.getElementById('userName').value.trim();
        const permissions = getSelectedPermissions(document);

        if (!email || !password || !role || !name) {
            alert('Please fill all fields.');
            return;
        }

        // Prevent duplicates
        const dup = await db.collection('admin_credentials').where('email','==',email).limit(1).get();
        if (!dup.empty) {
            alert('An admin with this email already exists.');
            return;
        }

        const hashedPassword = btoa(password);
        await db.collection('admin_credentials').add({
            email,
            password: hashedPassword,
            role,
            name,
            permissions,
            createdAt: firebase.firestore.FieldValue.serverTimestamp(),
            lastLogin: null,
            disabled: false
        });

        addUserForm.reset();
        alert('Admin user created.');
        loadUsers();
    }

    usersTableBody.addEventListener('click', async (e) => {
        const btn = e.target.closest('button[data-action]');
        if (!btn) return;
        const id = btn.getAttribute('data-id');
        const action = btn.getAttribute('data-action');
        const ref = db.collection('admin_credentials').doc(id);
        const docSnap = await ref.get();
        if (!docSnap.exists) return;
        const data = docSnap.data();

        // Security check: Prevent super admins from editing other super admins
        const currentUserRole = sessionStorage.getItem('adminRole');
        const targetUserRole = data.role || 'admin';
        const isCurrentUserSuperAdmin = currentUserRole === 'super_admin';
        const isTargetUserSuperAdmin = targetUserRole === 'super_admin';
        const isSameUser = sessionStorage.getItem('adminEmail') === data.email;
        
        if (isCurrentUserSuperAdmin && isTargetUserSuperAdmin && !isSameUser) {
            alert('You cannot modify other Super Admin accounts for security reasons.');
            return;
        }

        if (action === 'toggle') {
            await ref.update({ disabled: !data.disabled });
            loadUsers();
            return;
        }
        if (action === 'delete') {
            if (!confirm('Delete this admin?')) return;
            await ref.delete();
            loadUsers();
            return;
        }
        if (action === 'edit') {
            openEditModal(id, data);
            return;
        }
    });

    function openEditModal(id, data) {
        const modal = buildEditModal();
        modal.querySelector('#editUserId').value = id;
        modal.querySelector('#editUserName').value = data.name || '';
        modal.querySelector('#editUserRole').value = data.role || 'admin';
        const permBoxes = modal.querySelectorAll('input[type="checkbox"][data-permission]');
        const perms = new Set(Array.isArray(data.permissions) ? data.permissions : []);
        permBoxes.forEach(b => { b.checked = perms.has(b.getAttribute('data-permission')); });
        
        // Add role change listener to auto-set permissions
        const roleSelect = modal.querySelector('#editUserRole');
        roleSelect.addEventListener('change', function() {
            setPermissionsByRole(modal, this.value);
        });
        
        modal.style.display = 'flex';
    }
    
    function setPermissionsByRole(modal, role) {
        const permBoxes = modal.querySelectorAll('input[type="checkbox"][data-permission]');
        
        // Clear all permissions first
        permBoxes.forEach(box => { box.checked = false; });
        
        // Set permissions based on role
        switch(role) {
            case 'super_admin':
                permBoxes.forEach(box => { box.checked = true; });
                break;
            case 'events_admin':
                modal.querySelector('input[data-permission="events"]').checked = true;
                break;
            case 'locations_admin':
                modal.querySelector('input[data-permission="locations"]').checked = true;
                break;
            case 'viewer':
                // No permissions for viewer
                break;
        }
    }

    function buildEditModal() {
        let modal = document.getElementById('editUserModal');
        if (modal) return modal;
        modal = document.createElement('div');
        modal.id = 'editUserModal';
        modal.className = 'modal-overlay';
        modal.innerHTML = `
            <div class="modal-content">
                <div class="modal-header">
                    <h3>Edit User</h3>
                    <button class="close-btn" data-close>&times;</button>
                </div>
                <form id="editUserForm" class="user-form">
                    <input type="hidden" id="editUserId">
                    <div class="form-group">
                        <label for="editUserName">Display Name</label>
                        <input type="text" id="editUserName" required>
                    </div>
                    <div class="form-group">
                        <label for="editUserRole">Role</label>
                        <select id="editUserRole" required>
                            <option value="super_admin">Super Admin</option>
                            <option value="events_admin">Events Admin</option>
                            <option value="locations_admin">Locations Admin</option>
                            <option value="viewer">Viewer</option>
                        </select>
                    </div>
                    <div class="permissions-section">
                        <div class="permissions-header">
                            <h4>Permissions</h4>
                            <div class="permission-actions">
                                <button type="button" class="permission-btn" id="selectAllPerms">Select All</button>
                                <button type="button" class="permission-btn" id="clearAllPerms">Clear All</button>
                            </div>
                        </div>
                        <div class="permissions-grid">
                            <label class="permission-item">
                                <input type="checkbox" data-permission="events">
                                <span class="checkmark"></span>
                                Manage Events
                            </label>
                            <label class="permission-item">
                                <input type="checkbox" data-permission="locations">
                                <span class="checkmark"></span>
                                Manage Locations
                            </label>
                            <label class="permission-item">
                                <input type="checkbox" data-permission="users">
                                <span class="checkmark"></span>
                                Manage Users
                            </label>
                            <label class="permission-item">
                                <input type="checkbox" data-permission="settings">
                                <span class="checkmark"></span>
                                System Settings
                            </label>
                            <label class="permission-item">
                                <input type="checkbox" data-permission="export">
                                <span class="checkmark"></span>
                                Export Data
                            </label>
                            <label class="permission-item">
                                <input type="checkbox" data-permission="delete">
                                <span class="checkmark"></span>
                                Delete Data
                            </label>
                        </div>
                    </div>
                    <div class="modal-actions">
                        <button type="submit" class="save-btn">Save Changes</button>
                        <button type="button" class="cancel-btn" data-close>Cancel</button>
                    </div>
                </form>
            </div>`;
        document.body.appendChild(modal);
        modal.addEventListener('click', (e) => { if (e.target === modal || e.target.hasAttribute('data-close')) modal.style.display = 'none'; });
        modal.querySelector('#editUserForm').addEventListener('submit', saveEdit);
        
        // Add permission management buttons
        modal.querySelector('#selectAllPerms').addEventListener('click', function() {
            const permBoxes = modal.querySelectorAll('input[type="checkbox"][data-permission]');
            permBoxes.forEach(box => { box.checked = true; });
        });
        
        modal.querySelector('#clearAllPerms').addEventListener('click', function() {
            const permBoxes = modal.querySelectorAll('input[type="checkbox"][data-permission]');
            permBoxes.forEach(box => { box.checked = false; });
        });
        
        return modal;
    }

    async function saveEdit(e) {
        e.preventDefault();
        const id = document.getElementById('editUserId').value;
        const name = document.getElementById('editUserName').value.trim();
        const role = document.getElementById('editUserRole').value;
        const modal = document.getElementById('editUserModal');
        const permissions = getSelectedPermissions(modal);
        await db.collection('admin_credentials').doc(id).update({ name, role, permissions });
        modal.style.display = 'none';
        loadUsers();
    }

    addUserForm.addEventListener('submit', createUser);
    loadUsers();
})();


