// ── Utilities ────────────────────────────────────────────────────────────────
function setText(id, value, fallback = '—') {
    const el = document.getElementById(id);
    if (!el) return;
    el.textContent = value || fallback;
}
function openModal(scrimId, modalId) {
    document.getElementById(scrimId).classList.add('open');
    document.getElementById(modalId).classList.add('open');
}

function closeModal(scrimId, modalId) {
    document.getElementById(scrimId).classList.remove('open');
    document.getElementById(modalId).classList.remove('open');
}

function getClientLocationsUrl(clientId) {
    const url = new URL('/ClientLocations', window.location.origin);
    if (clientId) url.searchParams.set('SelectedClient', clientId);
    const searchInput = document.querySelector('.sp-index-search input[name="searchString"]');
    if (searchInput && searchInput.value.trim()) url.searchParams.set('searchString', searchInput.value.trim());
    return url.toString();
}

async function submitFormAndStayOnClient(form, clientId) {
    const resp = await fetch(form.action, {
        method: form.method || 'POST',
        body: new FormData(form),
        credentials: 'same-origin'
    });

    if (!resp.ok) {
        const text = await resp.text();
        console.error('POST failed:', {
            url: form.action,
            status: resp.status,
            body: text
        });

        alert('Akcija nije uspjela. Detalji su u Console logu.');
        return;
    }

    window.location.href = getClientLocationsUrl(clientId);
}

// ── Event delegation ─────────────────────────────────────────────────────────

function runClientLocationAction(action, p) {
    switch (action) {
        case 'open-details':
            openDetails(p);
            break;

        case 'edit-location':
            openEditLocation(p);
            break;

        case 'delete-location':
            openDeleteLocation(p);
            break;

        case 'create-location':
            openCreateLocation(p);
            break;

        case 'add-device':
            openAddDevice(p);
            break;

        case 'delete-device':
            openDeleteDevice(p);
            break;
    }
}

document.addEventListener('sp:action-menu', function (e) {
    runClientLocationAction(e.detail.action, e.detail.payload || {});
});

document.addEventListener('click', function (e) {
    const btn = e.target.closest('[data-action]');
    if (!btn) return;

    e.preventDefault();
    e.stopPropagation();

    if (window.closeAllMenus) {
        window.closeAllMenus();
    }

    const action = btn.dataset.action;
    const payload = btn.dataset.json ? JSON.parse(btn.dataset.json) : {};

    runClientLocationAction(action, payload);
});

// ── Details modal ─────────────────────────────────────────────────────────────

function openDetails(p) {
    const client = p.clientName || p.client || '';
    const location = p.locationName || p.location || p.name || '';

    setText('detailsLocationName', location, 'Pregled podataka lokacije');
    setText('detailsClientName', client, '');
    setText('detailsClient', client);
    setText('detailsLocation', location);
    setText('detailsCity', p.city);
    setText('detailsAddress', [p.address, p.postCode].filter(Boolean).join(', '));
    setText('detailsPhone', [p.tel1, p.tel2].filter(Boolean).join(' / '));
    setText('detailsDescription', p.description);
    setText('detailsDeviceCount', p.deviceCount || '0', '0');

    openModal('detailsScrim', 'detailsModal');
}

document.getElementById('btnCloseDetails').onclick  = () => closeModal('detailsScrim', 'detailsModal');
document.getElementById('btnCloseDetails2').onclick = () => closeModal('detailsScrim', 'detailsModal');
document.getElementById('detailsScrim').onclick     = () => closeModal('detailsScrim', 'detailsModal');

// ── Create location modal ─────────────────────────────────────────────────────

// Called from the header add button (no pre-computed payload - reads live DOM state)
function openCreateLocationFromHeader() {
    const selectedClientId = window.CL?.selectedClientId;
    if (!selectedClientId || selectedClientId === '00000000-0000-0000-0000-000000000000') {
        openCreateLocation({});
        return;
    }
    const headerSelect = document.querySelector('.sp-index-filter-form select[name="SelectedClient"]');
    const clientName = headerSelect?.selectedOptions?.[0]?.textContent?.trim() || '';
    openCreateLocation({ clientId: selectedClientId, clientName });
}

function openCreateLocation(p) {
    const form = document.getElementById('createLocationForm');
    const clientSelect = document.getElementById('createClientId');
    form.reset();
    clientSelect.value = p.clientId || '';
    const selectedText = clientSelect?.selectedOptions?.[0]?.textContent?.trim() || p.clientName || '';
    document.getElementById('createLocationSub').textContent = selectedText || 'Odaberite korisnika';
    openModal('createLocationScrim', 'createLocationModal');
}

document.getElementById('btnCloseCreateLocation').onclick  = () => closeModal('createLocationScrim', 'createLocationModal');
document.getElementById('btnCancelCreateLocation').onclick = () => closeModal('createLocationScrim', 'createLocationModal');
document.getElementById('createLocationScrim').onclick     = () => closeModal('createLocationScrim', 'createLocationModal');
document.getElementById('createLocationForm').addEventListener('submit', function (e) {
    e.preventDefault();
    submitFormAndStayOnClient(this, document.getElementById('createClientId').value);
});

// ── Edit location modal ───────────────────────────────────────────────────────

function openEditLocation(p) {
    document.getElementById('editLocationId').value    = p.id       || '';
    document.getElementById('editClientId').value      = p.clientId || '';
    document.getElementById('editLocationName').value  = p.name     || '';
    document.getElementById('editCity').value          = p.city     || '';
    document.getElementById('editAddress').value       = p.address  || '';
    document.getElementById('editPostCode').value      = p.postCode || '';
    document.getElementById('editTelephone1').value    = p.tel1     || '';
    document.getElementById('editTelephone2').value    = p.tel2     || '';
    document.getElementById('editDescription').value   = p.description || '';
    document.getElementById('editLocationSub').textContent = p.name || '';
    document.getElementById('editLocationForm').action = '/ClientLocations/Edit/' + p.id;
    openModal('editLocationScrim', 'editLocationModal');
}

document.getElementById('btnCloseEditLocation').onclick  = () => closeModal('editLocationScrim', 'editLocationModal');
document.getElementById('btnCancelEditLocation').onclick = () => closeModal('editLocationScrim', 'editLocationModal');
document.getElementById('editLocationScrim').onclick     = () => closeModal('editLocationScrim', 'editLocationModal');
document.getElementById('editLocationForm').addEventListener('submit', function (e) {
    e.preventDefault();
    submitFormAndStayOnClient(this, document.getElementById('editClientId').value);
});

// ── Add device modal ──────────────────────────────────────────────────────────

function resetAddDeviceSelects() {
    const modelSel = document.getElementById('addModel');
    const deviceSel = document.getElementById('addDevice');
    const addNewBtn = document.getElementById('btnAddNewDeviceFromLocation');
    modelSel.innerHTML  = '<option value="">Prvo odaberite proizvođača…</option>';  modelSel.disabled  = true;
    deviceSel.innerHTML = '<option value="">Prvo odaberite model…</option>';         deviceSel.disabled = true;
    if (addNewBtn) addNewBtn.disabled = true;
}

function openAddDevice(p) {
    document.getElementById('addDeviceForm').reset();
    document.getElementById('addDeviceLocationId').value = p.locationId   || '';
    document.getElementById('addDeviceClientId').value   = p.clientId     || '';
    document.getElementById('addDeviceSub').textContent  = p.locationName || '';
    document.getElementById('addInstallDate').value = new Date().toISOString().slice(0, 10);
    const guarantee = new Date(); guarantee.setFullYear(guarantee.getFullYear() + 2);
    document.getElementById('addGuaranteeDate').value = guarantee.toISOString().slice(0, 10);
    resetAddDeviceSelects();
    openModal('addDeviceScrim', 'addDeviceModal');
}

async function loadModels(manufacturerId) {
    const modelSel = document.getElementById('addModel');
    const deviceSel = document.getElementById('addDevice');
    const addNewBtn = document.getElementById('btnAddNewDeviceFromLocation');
    modelSel.innerHTML  = '<option value="">Učitavanje…</option>'; modelSel.disabled  = true;
    deviceSel.innerHTML = '<option value="">Prvo odaberite model…</option>'; deviceSel.disabled = true;
    if (addNewBtn) addNewBtn.disabled = true;
    if (!manufacturerId) { resetAddDeviceSelects(); return; }
    const data = await fetch('/DeviceInLocations/GetModelsList?SelectedManufacturer=' + encodeURIComponent(manufacturerId)).then(r => r.json());
    modelSel.innerHTML = '<option value="">Odaberite model…</option>';
    data.forEach(m => { const o = document.createElement('option'); o.value = m.id; o.textContent = m.name; modelSel.appendChild(o); });
    modelSel.disabled = false;
}

async function loadDevices(modelId, selectedDeviceId) {
    const deviceSel = document.getElementById('addDevice');
    const addNewBtn = document.getElementById('btnAddNewDeviceFromLocation');
    deviceSel.innerHTML = '<option value="">Učitavanje…</option>'; deviceSel.disabled = true;
    if (addNewBtn) addNewBtn.disabled = !modelId;
    if (!modelId) { deviceSel.innerHTML = '<option value="">Prvo odaberite model…</option>'; return; }
    const data = await fetch('/DeviceInLocations/GetDevicesList?SelectedModel=' + encodeURIComponent(modelId)).then(r => r.json());
    deviceSel.innerHTML = '<option value="">Odaberite uređaj…</option>';
    data.forEach(d => { const o = document.createElement('option'); o.value = d.id; o.textContent = d.serialNumber; deviceSel.appendChild(o); });
    deviceSel.disabled = false;
    if (selectedDeviceId) deviceSel.value = selectedDeviceId;
}

document.getElementById('btnCloseAddDevice').onclick  = () => closeModal('addDeviceScrim', 'addDeviceModal');
document.getElementById('btnCancelAddDevice').onclick = () => closeModal('addDeviceScrim', 'addDeviceModal');
document.getElementById('addDeviceScrim').onclick     = () => closeModal('addDeviceScrim', 'addDeviceModal');
document.getElementById('addManufacturer').addEventListener('change', function () { loadModels(this.value); });
document.getElementById('addModel').addEventListener('change', function () {
    loadDevices(this.value);
    document.getElementById('btnAddNewDeviceFromLocation').disabled = !this.value;
});
document.getElementById('addDeviceForm').addEventListener('submit', function (e) {
    e.preventDefault();
    submitFormAndStayOnClient(this, document.getElementById('addDeviceClientId').value);
});
document.getElementById('btnAddNewDeviceFromLocation').addEventListener('click', function () {
    const modelSel = document.getElementById('addModel');
    if (!modelSel.value) return;
    openCreateInlineDevice(modelSel.value, modelSel.selectedOptions[0]?.textContent || '');
});

// ── Create inline device modal ────────────────────────────────────────────────

function openCreateInlineDevice(modelId, modelName) {
    document.getElementById('createInlineDeviceForm').reset();
    document.getElementById('createInlineDeviceModelId').value = modelId || '';
    document.getElementById('createInlineDeviceSub').textContent = modelName || 'Odaberite model';
    document.getElementById('createInlineDeviceModelDisplay').textContent = modelName || '';
    document.getElementById('createInlineDeviceError').style.display = 'none';
    document.getElementById('createInlineDeviceError').textContent = '';
    openModal('createInlineDeviceScrim', 'createInlineDeviceModal');
    setTimeout(() => document.getElementById('createInlineDeviceSerial').focus(), 50);
}

document.getElementById('btnCloseCreateInlineDevice').onclick  = () => closeModal('createInlineDeviceScrim', 'createInlineDeviceModal');
document.getElementById('btnCancelCreateInlineDevice').onclick = () => closeModal('createInlineDeviceScrim', 'createInlineDeviceModal');
document.getElementById('createInlineDeviceScrim').onclick     = () => closeModal('createInlineDeviceScrim', 'createInlineDeviceModal');
document.getElementById('createInlineDeviceForm').addEventListener('submit', async function (e) {
    e.preventDefault();
    const modelId  = document.getElementById('createInlineDeviceModelId').value;
    const serial   = document.getElementById('createInlineDeviceSerial').value.trim();
    const errorEl  = document.getElementById('createInlineDeviceError');
    const saveBtn  = document.getElementById('btnSaveCreateInlineDevice');
    errorEl.style.display = 'none'; errorEl.textContent = '';
    if (!modelId || !serial) { errorEl.textContent = 'Model i serijski broj su obavezni.'; errorEl.style.display = 'block'; return; }
    saveBtn.disabled = true;
    try {
        const token = this.querySelector('input[name="__RequestVerificationToken"]')?.value
            || document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const body = new URLSearchParams();
        body.append('ModelID', modelId); body.append('SerialNumber', serial); body.append('__RequestVerificationToken', token || '');
        const resp = await fetch('/Devices/CreateInline', { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' }, body: body.toString(), credentials: 'same-origin' });
        const data = await resp.json().catch(() => null);
        if (!resp.ok || !data?.id) throw new Error(data?.message || 'Kreiranje uređaja nije uspjelo.');
        await loadDevices(modelId, data.id);
        closeModal('createInlineDeviceScrim', 'createInlineDeviceModal');
    } catch (err) {
        errorEl.textContent = err.message || 'Kreiranje uređaja nije uspjelo.';
        errorEl.style.display = 'block';
    } finally {
        saveBtn.disabled = false;
    }
});

// ── Delete location modal ─────────────────────────────────────────────────────

function openDeleteLocation(p) {
    document.getElementById('deleteLocationId').value = p.id || '';
    document.getElementById('deleteLocationClientId').value = p.clientId || '';
    document.getElementById('deleteLocationName').textContent = p.name || '';
    document.getElementById('deleteLocationConfirmCheck').checked = false;
    document.getElementById('btnConfirmDeleteLocation').disabled = true;
    document.getElementById('deleteLocationForm').action = '/ClientLocations/Delete/' + p.id;
    openModal('deleteLocationScrim', 'deleteLocationModal');
}

document.getElementById('btnCancelDeleteLocation').onclick = () => closeModal('deleteLocationScrim', 'deleteLocationModal');
document.getElementById('deleteLocationScrim').onclick     = () => closeModal('deleteLocationScrim', 'deleteLocationModal');
document.getElementById('deleteLocationConfirmCheck').addEventListener('change', function () {
    document.getElementById('btnConfirmDeleteLocation').disabled = !this.checked;
});
document.getElementById('deleteLocationForm').addEventListener('submit', function (e) {
    e.preventDefault();
    submitFormAndStayOnClient(this, document.getElementById('deleteLocationClientId').value);
});

// ── Delete device modal ───────────────────────────────────────────────────────

function openDeleteDevice(p) {
    document.getElementById('deleteDeviceId').value = p.id || '';
    document.getElementById('deleteDeviceClientId').value = p.clientId || '';
    document.getElementById('deleteDeviceSerial').textContent = p.serial || '';
    document.getElementById('deleteDeviceConfirmCheck').checked = false;
    document.getElementById('btnConfirmDeleteDevice').disabled = true;
    document.getElementById('deleteDeviceForm').action = '/DeviceInLocations/Delete/' + p.id;
    openModal('deleteDeviceScrim', 'deleteDeviceModal');
}

document.getElementById('btnCancelDeleteDevice').onclick = () => closeModal('deleteDeviceScrim', 'deleteDeviceModal');
document.getElementById('deleteDeviceScrim').onclick     = () => closeModal('deleteDeviceScrim', 'deleteDeviceModal');
document.getElementById('deleteDeviceConfirmCheck').addEventListener('change', function () {
    document.getElementById('btnConfirmDeleteDevice').disabled = !this.checked;
});
document.getElementById('deleteDeviceForm').addEventListener('submit', function (e) {
    e.preventDefault();
    submitFormAndStayOnClient(this, document.getElementById('deleteDeviceClientId').value);
});
