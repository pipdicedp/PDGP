$(document).ready(function () {

    function getApplicationId() {
        // Falls back across whichever hidden field is present on the current tab —
        // the main form field, or the tab partial's own hidden field.
        return $('#ApplicationId').val() || $('#hdnApplicationId').val();
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    // =========================================================
    // Photographs (Step 4)
    // =========================================================

    // Upload happens right when "Next" is clicked — before the existing
    // btnPhotoNext handler (in tl_tradelicence_apply.js) switches tabs, so the
    // files are safely stored before the user moves on.
    $('#btnPhotoNext').on('click', function () {

        var applicantInput = document.getElementById('ApplicantPhoto');
        var partnerInput = document.getElementById('PartnerPhoto');

        var applicantFile = applicantInput && applicantInput.files.length ? applicantInput.files[0] : null;
        var partnerFile = partnerInput && partnerInput.files.length ? partnerInput.files[0] : null;

        if (!applicantFile && !partnerFile) {
            return; // nothing new chosen — let the existing handler just move on
        }

        var applicationId = getApplicationId();
        if (!applicationId) {
            alert('Please save Application Details first.');
            return;
        }

        var formData = new FormData();
        formData.append('applicationId', applicationId);
        if (applicantFile) formData.append('applicantPhoto', applicantFile);
        if (partnerFile) formData.append('partnerPhoto', partnerFile);

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SavePhotographs',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            success: function () {
                alert('Photographs saved successfully.');
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.error) || 'Failed to upload photos. Please try again.';
                alert(msg);
            }
        });
    });

    // =========================================================
    // Documents (Step 5)
    // =========================================================

    // Maps each file input to its checklist document name and the short
    // suffix used by the matching Preview/Remove buttons in _UploadDocuments.cshtml
    // (e.g. #AadhaarFile -> btnPreviewAadhaar / btnRemoveAadhaar).
    var documentFields = [
        { inputId: 'AadhaarFile', shortName: 'Aadhaar', documentName: 'Aadhaar Copy' },
        { inputId: 'PropertyTaxFile', shortName: 'PropertyTax', documentName: 'Property Tax Receipt' },
        { inputId: 'BuildingPlanFile', shortName: 'BuildingPlan', documentName: 'Building Plan' }
    ];

    function uploadDocument(field) {
        var input = document.getElementById(field.inputId);
        var file = input && input.files.length ? input.files[0] : null;
        if (!file) return;

        var applicationId = getApplicationId();
        if (!applicationId) {
            alert('Please save Application Details first.');
            return;
        }

        var formData = new FormData();
        formData.append('applicationId', applicationId);
        formData.append('documentName', field.documentName);
        formData.append('file', file);

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SaveDocument',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            success: function (data) {
                $('#btnPreview' + field.shortName).prop('disabled', false).data('documentId', data.documentId);
                $('#btnRemove' + field.shortName).prop('disabled', false).data('documentId', data.documentId);
                alert(field.documentName + ' uploaded successfully.');
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.error) || 'Failed to upload document. Please try again.';
                alert(msg);
            }
        });
    }

    documentFields.forEach(function (field) {
        $('#' + field.inputId).on('change', function () {
            uploadDocument(field);
        });
    });

    // ---- Preview: open the stored (decrypted) file in the modal iframe ----
    $(document).on('click', '[id^="btnPreview"]', function () {
        var docId = $(this).data('documentId');
        if (!docId) return;

        $('#documentViewer').attr('src', '/TradeLicence/NewLicence/Apply/ViewDocument?documentId=' + docId);

        var modalEl = document.getElementById('documentPreviewModal');
        if (modalEl && window.bootstrap) {
            new bootstrap.Modal(modalEl).show();
        }
    });

    // ---- Remove: deletes the row from the database, resets that upload slot ----
    $(document).on('click', '[id^="btnRemove"]', function () {
        var $btn = $(this);
        var docId = $btn.data('documentId');
        if (!docId) return;

        if (!confirm('Remove this document?')) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/DeleteDocument',
            type: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: { documentId: docId },
            success: function () {
                var shortName = $btn.attr('id').replace('btnRemove', '');
                $('#' + shortName + 'File').val('');
                $('#btnPreview' + shortName).prop('disabled', true).removeData('documentId');
                $btn.prop('disabled', true).removeData('documentId');
            },
            error: function () {
                alert('Failed to remove document. Please try again.');
            }
        });
    });

});