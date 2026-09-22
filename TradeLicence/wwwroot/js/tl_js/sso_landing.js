(function () {
    var hamburgerBtn = document.getElementById('ssoHamburgerBtn');
    var sidebar = document.getElementById('ssoServicesSidebar');
    var overlay = document.getElementById('ssoSidebarOverlay');
    var closeBtn = document.getElementById('ssoSidebarClose');

    if (!hamburgerBtn || !sidebar || !overlay) return;

    function openSidebar() {
        sidebar.classList.add('is-open');
        overlay.classList.add('is-active');
        hamburgerBtn.setAttribute('aria-expanded', 'true');
        sidebar.setAttribute('aria-hidden', 'false');
    }

    function closeSidebar() {
        sidebar.classList.remove('is-open');
        overlay.classList.remove('is-active');
        hamburgerBtn.setAttribute('aria-expanded', 'false');
        sidebar.setAttribute('aria-hidden', 'true');
    }

    function toggleSidebar() {
        if (sidebar.classList.contains('is-open')) {
            closeSidebar();
        } else {
            openSidebar();
        }
    }

    hamburgerBtn.addEventListener('click', toggleSidebar);
    overlay.addEventListener('click', closeSidebar);
    if (closeBtn) closeBtn.addEventListener('click', closeSidebar);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeSidebar();
    });
})();
