// edit-dil.js  (simplified - location + dates + comment only)
// Depends on: sp-styles.css, index-table.js (closeAllMenus, sp:action-menu)

(function () {

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

    async function populateLocations(clientId, selectedId) {
        const sel = document.getElementById('editDilLocation');
        sel.innerHTML = '<option value="">Učitavanje…</option>';
        sel.disabled  = true;

        if (!clientId) {
            sel.innerHTML = '<option value="">Nema lokacija</option>';
            return;
        }

        try {
            const data = await fetch('/DeviceInLocations/GetLocationsList?clientId=' + clientId).then(r => r.json());
            sel.innerHTML = '<option value="">Odaberite lokaciju…</option>';
            data.forEach(function (l) {
                const o       = document.createElement('option');
                o.value       = l.id;
                o.textContent = l.name;
                if (selectedId && l.id.toString().toLowerCase() === selectedId.toString().toLowerCase())
                    o.selected = true;
                sel.appendChild(o);
            });
        } catch {
            sel.innerHTML = '<option value="">Greška pri učitavanju</option>';
        }

        sel.disabled = false;
    }

    async function openEditDil(p) {
        setError('');
        document.getElementById('btnSaveEditDil').disabled = false;
        document.getElementById('editDilSub').textContent  = p.serial || '';
        document.getElementById('editDilLocation').innerHTML = '<option value="">Učitavanje…</option>';
        document.getElementById('editDilLocation').disabled  = true;

        openModal('editDilScrim', 'editDilModal');

        let d;
        try {
            const resp = await fetch('/DeviceInLocations/GetEditJson/' + p.id);
            if (!resp.ok) { setError('Greška pri učitavanju podataka.'); return; }
            d = await resp.json();
        } catch {
            setError('Greška pri učitavanju podataka.');
            return;
        }

        document.getElementById('editDilId').value            = d.id;
        document.getElementById('editDilInstallDate').value   = d.installDate   || '';
        document.getElementById('editDilGuaranteeDate').value = d.guaranteeDate || '';
        document.getElementById('editDilDescription').value   = d.description   || '';
        document.getElementById('editDilSub').textContent     = d.serial        || p.serial || '';

        await populateLocations(d.clientId, d.locationId);
    }

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

            const data = await resp.json().catch(() => null);

            if (!resp.ok || (data && data.success === false)) {
                setError((data && data.message) || 'Greška pri snimanju.');
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

    document.getElementById('btnCloseEditDil').onclick  = () => closeModal('editDilScrim', 'editDilModal');
    document.getElementById('btnCancelEditDil').onclick = () => closeModal('editDilScrim', 'editDilModal');
    document.getElementById('editDilScrim').onclick     = () => closeModal('editDilScrim', 'editDilModal');

    document.addEventListener('sp:action-menu', function (e) {
        if (e.detail.action === 'edit-dil') openEditDil(e.detail.payload || {});
    });

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-action="edit-dil"]');
        if (!btn) return;
        e.preventDefault();
        e.stopPropagation();
        if (window.closeAllMenus) window.closeAllMenus();
        openEditDil(btn.dataset.json ? JSON.parse(btn.dataset.json) : {});
    });

})();
