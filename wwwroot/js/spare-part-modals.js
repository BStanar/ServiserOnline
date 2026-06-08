// spare-part-modals.js
// Shared JS for _EditSparePartModal and _DeleteSparePartModal.
// Include once on any page that renders those partials.
// Depends on: index-table.js (closeAllMenus)

(function () {

    // ── Modal helpers ─────────────────────────────────────────────────────────
    function openModal(scrimId, modalId) {
        document.getElementById(scrimId)?.classList.add('open');
        document.getElementById(modalId)?.classList.add('open');
    }
    function closeModal(scrimId, modalId) {
        document.getElementById(scrimId)?.classList.remove('open');
        document.getElementById(modalId)?.classList.remove('open');
    }

    // ── Cascading model filter (filter select options by mfr) ─────────────────
    function filterSpModels(mfrId) {
        const sel = document.getElementById('editSpModelId');
        if (!sel) return;
        Array.from(sel.options).forEach(function (o) {
            if (!o.value) return;
            o.hidden = mfrId ? o.dataset.mfr !== mfrId : false;
        });
    }

    // ── Edit modal ────────────────────────────────────────────────────────────
    // Call openEditSparePart(dataset) where dataset has:
    // id, name, sku, catalog, stock, price, modelId
    window.openEditSparePart = function (p) {
        if (window.closeAllMenus) window.closeAllMenus();

        document.getElementById('editSpId').value      = p.id        || '';
        document.getElementById('editSpName').value    = p.name      || '';
        document.getElementById('editSpSku').value     = p.sku       || '';
        document.getElementById('editSpCatalog').value = p.catalog   || '';
        document.getElementById('editSpStock').value   = p.stock     ?? '';
        document.getElementById('editSpPrice').value   = p.price     ?? '';
        document.getElementById('editSpSub').textContent = p.name    || '';

        const modelOpt = document.querySelector('#editSpModelId option[value="' + p.modelId + '"]');
        const mfrName  = modelOpt?.dataset.mfrName || '';
        const mfrId    = modelOpt?.dataset.mfr     || '';

        document.getElementById('editSpMfrDisplay').textContent = mfrName;
        filterSpModels(mfrId);
        document.getElementById('editSpModelId').value = p.modelId || '';

        openModal('editSpScrim', 'editSpModal');
    };

    // Wire close buttons
    document.getElementById('btnCloseEditSp')?.addEventListener('click',  () => closeModal('editSpScrim', 'editSpModal'));
    document.getElementById('btnCancelEditSp')?.addEventListener('click', () => closeModal('editSpScrim', 'editSpModal'));
    document.getElementById('editSpScrim')?.addEventListener('click',     () => closeModal('editSpScrim', 'editSpModal'));

    // Delegate: any element with data-action="edit-sp" dispatched via sp:action-menu
    document.addEventListener('sp:action-menu', function (e) {
        if (e.detail.action === 'edit-sp') window.openEditSparePart(e.detail.payload || {});
        if (e.detail.action === 'delete-sp') window.openDeleteSparePart(e.detail.payload || {});
    });

    // Delegate: direct data-action="edit-sp" clicks (outside ActionMenu)
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-action="edit-sp"]');
        if (!btn) return;
        e.preventDefault();
        e.stopPropagation();
        window.openEditSparePart(btn.dataset.json ? JSON.parse(btn.dataset.json) : {});
    });

    // ── Delete modal ──────────────────────────────────────────────────────────
    // Call openDeleteSparePart(dataset) where dataset has: id, name, sku
    window.openDeleteSparePart = function (p) {
        if (window.closeAllMenus) window.closeAllMenus();

        document.getElementById('deleteSpId').value           = p.id   || '';
        document.getElementById('deleteSpName').textContent   = p.name || '';
        document.getElementById('deleteSpSku').textContent    = p.sku  ? '(' + p.sku + ')' : '';
        document.getElementById('deleteSpConfirm').checked    = false;
        document.getElementById('btnConfirmDeleteSp').disabled = true;
        document.getElementById('deleteSpForm').action = '/SpareParts/Delete/' + (p.id || '');

        openModal('deleteSpScrim', 'deleteSpModal');
    };

    document.getElementById('deleteSpConfirm')?.addEventListener('change', function () {
        document.getElementById('btnConfirmDeleteSp').disabled = !this.checked;
    });

    document.getElementById('btnCloseDeleteSp')?.addEventListener('click',  () => closeModal('deleteSpScrim', 'deleteSpModal'));
    document.getElementById('btnCancelDeleteSp')?.addEventListener('click', () => closeModal('deleteSpScrim', 'deleteSpModal'));
    document.getElementById('deleteSpScrim')?.addEventListener('click',     () => closeModal('deleteSpScrim', 'deleteSpModal'));

    // Delegate: direct data-action="delete-sp" clicks
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-action="delete-sp"]');
        if (!btn) return;
        e.preventDefault();
        e.stopPropagation();
        window.openDeleteSparePart(btn.dataset.json ? JSON.parse(btn.dataset.json) : {});
    });

    // ── Escape ────────────────────────────────────────────────────────────────
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        closeModal('editSpScrim',   'editSpModal');
        closeModal('deleteSpScrim', 'deleteSpModal');
    });

})();
