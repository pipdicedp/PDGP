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

document.getElementById('btnReturnToApplicant').addEventListener('click', function () {
    Swal.fire({
        title: 'Return to Applicant',
        text: 'Tell the applicant what needs to be corrected.',
        input: 'textarea',
        inputPlaceholder: 'Reason / remarks...',
        showCancelButton: true,
        confirmButtonText: 'Return Application',
        confirmButtonColor: '#D4A017',
        inputValidator: function (value) {
            if (!value || !value.trim()) {
                return 'Please enter a reason before returning the application.';
            }
        }
    }).then(function (result) {
        if (result.isConfirmed) {
            document.getElementById('returnRemarks').value = result.value;
            document.getElementById('returnForm').submit();
        }
    });
});

document.getElementById('btnForwardToGM').addEventListener('click', function () {
    Swal.fire({
        title: 'Forward to GM',
        text: 'Optional remarks for the General Manager.',
        input: 'textarea',
        inputPlaceholder: 'Remarks (optional)...',
        showCancelButton: true,
        confirmButtonText: 'Forward',
        confirmButtonColor: '#1a3a52'
    }).then(function (result) {
        if (result.isConfirmed) {
            document.getElementById('forwardRemarks').value = result.value || '';
            document.getElementById('forwardForm').submit();
        }
    });
});
