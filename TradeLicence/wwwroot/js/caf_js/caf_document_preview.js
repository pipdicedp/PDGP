// Opens an uploaded CAF document inline in a modal (no download) — same look and behaviour as the Trade Licence
// "Document Preview" popup. Used by the CAF Application Preview (applicant), the Industry officer's view and the
// department officer's view; all three render _CafPreviewSections, which carries the modal markup.
//
// The link's data-doc-url already points at the right Document action for whoever is looking (applicant /
// Industry officer / department officer), so this script never builds a URL itself.
(function () {
    document.addEventListener('click', function (e) {
        var link = e.target.closest('.caf-doc-view');
        if (!link) return;
        e.preventDefault();

        var url = link.getAttribute('data-doc-url');
        if (!url) return;

        var modalEl = document.getElementById('cafDocumentModal');
        var viewer = document.getElementById('cafDocumentViewer');

        // No Bootstrap / no modal on the page: fall back to a new tab rather than doing nothing.
        if (!modalEl || !viewer || !window.bootstrap) {
            window.open(url, '_blank', 'noopener');
            return;
        }

        // Officers' sections are collapsible/overflow containers — a modal left inside one can be clipped or
        // sit under the backdrop, so it lives directly under <body>.
        if (modalEl.parentElement !== document.body) {
            document.body.appendChild(modalEl);
        }

        var title = document.getElementById('cafDocumentModalTitle');
        if (title) title.textContent = link.getAttribute('data-doc-title') || 'Document Preview';

        viewer.src = url;
        window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
    });

    // Stop a PDF from playing on in the background once the popup is closed.
    document.addEventListener('hidden.bs.modal', function (e) {
        if (e.target && e.target.id === 'cafDocumentModal') {
            var viewer = document.getElementById('cafDocumentViewer');
            if (viewer) viewer.src = 'about:blank';
        }
    });
})();
