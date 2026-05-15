// ── Modal helpers ───────────────────────────────────────────────────────────
function openModal(scrimId, modalId) {
    document.getElementById(scrimId).classList.add('open');
    document.getElementById(modalId).classList.add('open');
}
function closeModal(scrimId, modalId) {
    document.getElementById(scrimId).classList.remove('open');
    document.getElementById(modalId).classList.remove('open');
}

// ── Action router ────────────────────────────────────────────────────────────
document.addEventListener('sp:action-menu', function (e) {
    runAction(e.detail.action, e.detail.payload || {});
});

document.addEventListener('click', function (e) {
    const btn = e.target.closest('[data-action]');
    if (!btn) return;
    e.preventDefault();
    e.stopPropagation();
    if (window.closeAllMenus) window.closeAllMenus();
    runAction(btn.dataset.action, btn.dataset.json ? JSON.parse(btn.dataset.json) : {});
});

function runAction(action, p) {
    switch (action) {
        case 'open-details': openDetails(p); break;
        case 'delete-dil':   openDeleteDil(p); break;
    }
}

// ── Details modal ────────────────────────────────────────────────────────────
function setText(id, val) {
    const el = document.getElementById(id);
    if (el) el.textContent = val || '—';
}

function openDetails(p) {
    setText('detailsTitle',    p.serial);
    setText('detailsSubtitle', p.model);
    setText('detailsLocation', p.location + (p.locationCity ? ', ' + p.locationCity : ''));
    setText('detailsInstall',  p.install);
    setText('detailsGuarantee', p.guarantee);
    document.getElementById('detailsDescription').textContent = p.description || '—';
    document.getElementById('detailsEditLink').href = p.editUrl || '#';

    const delBtn = document.getElementById('detailsDeleteBtn');
    delBtn.dataset.id       = p.id;
    delBtn.dataset.serial   = p.serial   || '';
    delBtn.dataset.location = p.location || '';

    openModal('detailsScrim', 'detailsModal');
}

document.getElementById('detailsDeleteBtn').addEventListener('click', function () {
    closeModal('detailsScrim', 'detailsModal');
    openDeleteDil({
        id:       this.dataset.id,
        serial:   this.dataset.serial,
        location: this.dataset.location
    });
});

// ── Delete modal ─────────────────────────────────────────────────────────────
function openDeleteDil(p) {
    document.getElementById('deleteId').value = p.id;
    setText('deleteSerial',   p.serial);
    setText('deleteLocation', p.location);

    const check = document.getElementById('deleteConfirmCheck');
    check.checked = false;
    document.getElementById('btnConfirmDelete').disabled = true;

    document.getElementById('deleteForm').action =
        window.PAGE.deleteBaseUrl + '/' + p.id;

    openModal('deleteScrim', 'deleteModal');
}

document.getElementById('deleteConfirmCheck').addEventListener('change', function () {
    document.getElementById('btnConfirmDelete').disabled = !this.checked;
});
