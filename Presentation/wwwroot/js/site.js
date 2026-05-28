(() => {
    const body = document.body;

    // ── Mobile sidebar (overlay) ──────────────────────────────
    const toggle = document.querySelector('[data-shell-toggle]');
    const closeButtons = document.querySelectorAll('[data-shell-close]');

    if (toggle) {
        toggle.addEventListener('click', () => {
            // On desktop, toggle is for collapsing; on mobile, it opens overlay
            if (window.innerWidth >= 992) {
                // handled by sidebar-collapse logic below
            } else {
                const isOpen = body.classList.toggle('shell-open');
                toggle.setAttribute('aria-expanded', String(isOpen));
            }
        });
    }

    closeButtons.forEach((button) => {
        button.addEventListener('click', () => {
            body.classList.remove('shell-open');
            toggle?.setAttribute('aria-expanded', 'false');
        });
    });

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            body.classList.remove('shell-open');
            toggle?.setAttribute('aria-expanded', 'false');
        }
    });

    // ── Desktop sidebar collapse ──────────────────────────────
    const collapseBtn = document.querySelector('[data-sidebar-collapse]');
    const STORAGE_KEY = 'sidebar_collapsed';

    // Restore persisted state
    if (localStorage.getItem(STORAGE_KEY) === '1') {
        body.classList.add('sidebar-collapsed');
    }

    if (collapseBtn) {
        collapseBtn.addEventListener('click', () => {
            const isNowCollapsed = body.classList.toggle('sidebar-collapsed');
            localStorage.setItem(STORAGE_KEY, isNowCollapsed ? '1' : '0');
        });
    }

    // Topbar hamburger on desktop triggers collapse (not overlay)
    if (toggle) {
        toggle.addEventListener('click', () => {
            if (window.innerWidth >= 992) {
                const isNowCollapsed = body.classList.toggle('sidebar-collapsed');
                localStorage.setItem(STORAGE_KEY, isNowCollapsed ? '1' : '0');
            }
        });
    }

    // ── Upload input ──────────────────────────────────────────
    const uploadInput = document.querySelector('[data-upload-input]');
    const uploadName = document.querySelector('[data-upload-name]');
    const uploadSize = document.querySelector('[data-upload-size]');
    const uploadPreview = document.querySelector('[data-upload-preview]');
    const uploadEmpty = document.querySelector('[data-upload-empty]');

    if (uploadInput && uploadName && uploadSize && uploadPreview) {
        uploadInput.addEventListener('change', () => {
            const file = uploadInput.files?.[0];
            if (!file) {
                uploadPreview.classList.add('d-none');
                uploadEmpty?.classList.remove('d-none');
                return;
            }

            uploadName.textContent = file.name;
            uploadSize.textContent = formatFileSize(file.size);
            uploadPreview.classList.remove('d-none');
            uploadEmpty?.classList.add('d-none');
        });
    }

    function formatFileSize(bytes) {
        if (bytes < 1024) return `${bytes} B`;
        const kilobytes = bytes / 1024;
        if (kilobytes < 1024) return `${kilobytes.toFixed(1)} KB`;
        return `${(kilobytes / 1024).toFixed(1)} MB`;
    }
})();
