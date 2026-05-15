// edit-dil.js
// Include on any page that uses _EditDilModal.cshtml
// Depends on: sp-styles.css (modal CSS), index-table.js (closeAllMenus, sp:action-menu dispatch)

(function () {
    // ── Modal helpers (local, in case index-table.js not present e.g. ServiceHistory) ──
    function openModal(scrimId, modalId) {
        document.getElementById(scrimId)?.classList.add('open');
        document.getElementById(modalId)?.classList.add('open');
    }
    function closeModal(scrimId, modalId) {
        document.getElementById(scrimId)?.classList.remove('open');
        document.getElementById(modalId)?.classList.remove('open');
    }

    function setError(msg) {
        const el = document.getElementById('editDilError');
        if (!el) return;
        if (msg) { el.textContent = msg; el.style.display = 'block'; }
        else     { el.textContent = ''; el.style.display = 'none'; }
    }

    // Populates a <select> from a JSON endpoint [{id, name}]
    // Skips fetch if url is falsy; pre-selects selectedId if provided
    async function populateSelect(sel, url, selectedId) {
        sel.innerHTML = '<option value="">Učitavanje…</option>';
        sel.disabled  = true;
        if (!url) {
            sel.innerHTML = '<option value="">—</option>';
            return;
        }
        const data = await fetch(url).then(r => r.json());
        sel.innerHTML = '<option value="">Odaberite…</option>';
        data.forEach(d => {
            const o       = document.createElement('option');
            o.value       = d.id;
            o.textContent = d.name ?? d.serialNumber ?? String(d.id);
            if (selectedId && d.id.toString().toLowerCase() === selectedId.toString().toLowerCase())
                o.selected = true;
            sel.appendChild(o);
        });
        sel.disabled = false;
    }

    // ── Open & populate modal ─────────────────────────────────────────────────
    async function openEditDil(p) {
        setError('');
        document.getElementById('btnSaveEditDil').disabled = false;
        document.getElementById('editDilSub').textContent  = p.serial || '';

        // Reset cascading selects
        const mdlSel = document.getElementById('editDilModel');
        const devSel = document.getElementById('editDilDevice');
        mdlSel.innerHTML = '<option value="">Prvo odaberite proizvođača…</option>';
        mdlSel.disabled  = true;
        devSel.innerHTML = '<option value="">Prvo odaberite model…</option>';
        devSel.disabled  = true;

        openModal('editDilScrim', 'editDilModal');

        // Fetch current record data
        let d;
        try {
            const resp = await fetch('/DeviceInLocations/GetEditJson/' + p.id);
            if (!resp.ok) { setError('Greška pri učitavanju podataka.'); return; }
            d = await resp.json();
        } catch {
            setError('Greška pri učitavanju podataka.');
            return;
        }

        // Scalar fields
        document.getElementById('editDilId').value          = d.id;
        document.getElementById('editDilClientId').value    = d.clientId    || '';
        document.getElementById('editDilInstallDate').value = d.installDate  || '';
        document.getElementById('editDilGuaranteeDate').value = d.guaranteeDate || '';
        document.getElementById('editDilDescription').value = d.description  || '';

        // Load locations + manufacturers in parallel
        const locSel = document.getElementById('editDilLocation');
        const mfrSel = document.getElementById('editDilManufacturer');

        await Promise.all([
            populateSelect(
                locSel,
                d.clientId
                    ? '/DeviceInLocations/GetLocationsList?clientId=' + d.clientId
                    : null,
                d.locationId
            ),
            populateSelect(
                mfrSel,
                '/DeviceInLocations/GetManufacturersList',
                d.manufacturerId
            )
        ]);

        // Cascade: models (serial, wait for manufacturer)
        if (d.manufacturerId) {
            await populateSelect(
                mdlSel,
                '/DeviceInLocations/GetModelsList?SelectedManufacturer=' + d.manufacturerId,
                d.modelId
            );
        }

        // Cascade: devices
        if (d.modelId) {
            await populateSelect(
                devSel,
                '/DeviceInLocations/GetDevicesList?SelectedModel=' + d.modelId,
                d.deviceId
            );
        }
    }

    // ── Cascade change listeners ──────────────────────────────────────────────
    document.getElementById('editDilManufacturer').addEventListener('change', async function () {
        const mdlSel = document.getElementById('editDilModel');
        const devSel = document.getElementById('editDilDevice');
        devSel.innerHTML = '<option value="">Prvo odaberite model…</option>';
        devSel.disabled  = true;
        if (!this.value) {
            mdlSel.innerHTML = '<option value="">Prvo odaberite proizvođača…</option>';
            mdlSel.disabled  = true;
            return;
        }
        await populateSelect(mdlSel,
            '/DeviceInLocations/GetModelsList?SelectedManufacturer=' + this.value, null);
    });

    document.getElementById('editDilModel').addEventListener('change', async function () {
        const devSel = document.getElementById('editDilDevice');
        if (!this.value) {
            devSel.innerHTML = '<option value="">Prvo odaberite model…</option>';
            devSel.disabled  = true;
            return;
        }
        await populateSelect(devSel,
            '/DeviceInLocations/GetDevicesList?SelectedModel=' + this.value, null);
    });

    // ── Form submit (AJAX) ────────────────────────────────────────────────────
    document.getElementById('editDilForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        setError('');
        const saveBtn = document.getElementById('btnSaveEditDil');
        saveBtn.disabled = true;
        try {
            const resp = await fetch(this.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: new FormData(this),
                credentials: 'same-origin'
            });
            if (!resp.ok) {
                const data = await resp.json().catch(() => null);
                setError(data?.message || 'Greška pri snimanju.');
                return;
            }
            closeModal('editDilScrim', 'editDilModal');
            window.location.reload();
        } catch {
            setError('Greška pri snimanju.');
        } finally {
            saveBtn.disabled = false;
        }
    });

    // ── Close ─────────────────────────────────────────────────────────────────
    document.getElementById('btnCloseEditDil').onclick  = () => closeModal('editDilScrim', 'editDilModal');
    document.getElementById('btnCancelEditDil').onclick = () => closeModal('editDilScrim', 'editDilModal');
    document.getElementById('editDilScrim').onclick     = () => closeModal('editDilScrim', 'editDilModal');

    // ── Action routing ────────────────────────────────────────────────────────
    // Handles dispatch from handleActionMenuItem (ActionMenu dropdown buttons)
    document.addEventListener('sp:action-menu', function (e) {
        if (e.detail.action === 'edit-dil') openEditDil(e.detail.payload || {});
    });

    // Handles direct data-action="edit-dil" clicks (e.g. standalone buttons on ServiceHistory)
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-action="edit-dil"]');
        if (!btn) return;
        e.preventDefault();
        e.stopPropagation();
        if (window.closeAllMenus) window.closeAllMenus();
        openEditDil(btn.dataset.json ? JSON.parse(btn.dataset.json) : {});
    });
})();
