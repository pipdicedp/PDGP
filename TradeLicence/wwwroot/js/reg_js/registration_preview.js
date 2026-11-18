// Opens a citizen-uploaded document inside the shared popup (#regDocModal) instead of a new
// tab/page. Delegated on document so it also works for rows added or re-rendered later.
// Same behaviour as wwwroot/js/tl_js/tl_document_preview_modal.js in Trade Licence.
(function () {
    'use strict';

    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('.reg-view-doc');
        if (!trigger) return;
        e.preventDefault();

        var url = trigger.getAttribute('data-doc-url');
        var modalEl = document.getElementById('regDocModal');
        var viewer = document.getElementById('regDocViewer');
        if (!url || !modalEl || !viewer) return;

        var title = document.getElementById('regDocTitle');
        if (title) title.textContent = trigger.getAttribute('data-doc-title') || 'Document Preview';

        viewer.src = url;
        if (window.bootstrap) bootstrap.Modal.getOrCreateInstance(modalEl).show();
    });

    var modalEl = document.getElementById('regDocModal');
    if (modalEl) {
        // Drop the loaded file when the popup closes so nothing keeps running behind it.
        modalEl.addEventListener('hidden.bs.modal', function () {
            var viewer = document.getElementById('regDocViewer');
            if (viewer) viewer.src = 'about:blank';
        });
    }
})();
