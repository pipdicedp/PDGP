// Opens a document inline in a modal (no download). Delegated on document
// so it works for content injected later via AJAX (citizen Preview tab) as
// well as content rendered directly server-side (officer view).
// Requires a #previewDocumentModal / #previewDocumentViewer pair to exist
// somewhere on the page — see _PreviewApplication.cshtml.
$(document).on('click', '.btn-preview-view-doc', function (e) {
    e.preventDefault();
    var docId = $(this).data('documentId');
    if (!docId) return;

    var viewer = document.getElementById('previewDocumentViewer');
    if (viewer) viewer.src = '/TradeLicence/NewLicence/Apply/ViewDocument?documentId=' + docId;

    var modalEl = document.getElementById('previewDocumentModal');
    if (modalEl && window.bootstrap) {
        new bootstrap.Modal(modalEl).show();
    }
});
