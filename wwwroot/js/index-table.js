(function () {
    window.closeAllMenus = function () {
        document.querySelectorAll('.sp-more-menu.open').forEach(menu => {
            menu.classList.remove('open');
            menu.style.position = '';
            menu.style.top = '';
            menu.style.bottom = '';
            menu.style.right = '';
            menu.style.left = '';
            menu.style.visibility = '';
        });
    };

    window.toggleMoreMenu = function (button, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }

        const wrap = button.closest('.sp-more-wrap');
        if (!wrap) return;

        const menu = wrap.querySelector('.sp-more-menu');
        if (!menu) return;

        const wasOpen = menu.classList.contains('open');

        window.closeAllMenus();

        if (!wasOpen) {
            const rect = button.getBoundingClientRect();
            const spaceBelow = window.innerHeight - rect.bottom;

            menu.style.position = 'fixed';
            menu.style.right = (window.innerWidth - rect.right) + 'px';
            menu.style.left = 'auto';

            if (spaceBelow < 120 && rect.top > spaceBelow) {
                menu.style.bottom = (window.innerHeight - rect.top) + 'px';
                menu.style.top = 'auto';
            } else {
                menu.style.top = (rect.bottom + 4) + 'px';
                menu.style.bottom = 'auto';
            }

            menu.classList.add('open');
        }
    };

    window.toggleGroup = function (groupKey) {
        if (!groupKey) return;

        const rows = document.querySelectorAll('.group-' + CSS.escape(groupKey));
        const arrow = document.getElementById('arrow-' + groupKey);
        const isOpen = Array.from(rows).some(row => row.style.display !== 'none');

        rows.forEach(row => {
            row.style.display = isOpen ? 'none' : '';
        });

        if (arrow) {
            arrow.classList.toggle('collapsed', isOpen);
        }
    };

    window.toggleModel = function (modelKey) {
        if (!modelKey) return;

        const rows = document.querySelectorAll('.group-model-' + CSS.escape(modelKey));
        const arrow = document.getElementById('arrow-' + modelKey);
        const isOpen = Array.from(rows).some(row => row.style.display !== 'none');

        rows.forEach(row => {
            row.style.display = isOpen ? 'none' : '';
        });

        if (arrow) {
            arrow.classList.toggle('collapsed', isOpen);
        }
    };

    window.handleActionMenuItem = function (button, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }

        window.closeAllMenus();

        const action = button.dataset.action;
        const payload = button.dataset.json ? JSON.parse(button.dataset.json) : {};

        document.dispatchEvent(new CustomEvent('sp:action-menu', {
            detail: {
                action: action,
                payload: payload,
                source: button
            }
        }));
    };

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.sp-more-wrap')) {
            window.closeAllMenus();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            window.closeAllMenus();
        }
    });
    document.addEventListener('scroll', function () {
        window.closeAllMenus();
    }, { capture: true, passive: true });

    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('[data-modal-close]');
        if (!trigger) return;
        const [scrimId, modalId] = trigger.dataset.modalClose.split(',');
        closeModal(scrimId, modalId);
    });

    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('[data-modal-open]');
        if (!trigger) return;
        const [scrimId, modalId] = trigger.dataset.modalOpen.split(',');
        openModal(scrimId, modalId);
    });

})();