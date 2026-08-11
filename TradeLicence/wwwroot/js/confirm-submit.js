$(document).ready(function () {

    function getApplicationId() {
        return $('#ApplicationId').val() || $('#hdnApplicationId').val();
    }

    // Exposed on window so other tab scripts (e.g. shop-establishment.js) can
    // call it right after they save, keeping the Confirm tab always fresh.
    window.populateConfirmSummary = function () {
        var applicationId = getApplicationId();
        if (!applicationId) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/GetApplicationSummary',
            type: 'GET',
            data: { applicationId: applicationId },
            success: function (data) {
                $('#lblApplicantName').text(data.applicantName || '-');
                $('#lblTradeName').text(data.tradeName || '-');
                $('#lblMobileNumber').text(data.mobileNumber || '-');
            }
        });
    };

    // Covers reopening a draft that's already sitting on the Confirm step
    populateConfirmSummary();

    // Also refresh right before showing the Confirm tab, whichever tab link
    // leads there (matches the wizard tab markup already used elsewhere).
    $(document).on('click', '.tl-wizard-tab[href*="confirm"], [data-tab="confirm"]', function () {
        populateConfirmSummary();
    });

    // ---- Final submit: confirm, then AJAX-submit the whole form and download
    // the PDF acknowledgement that comes back — no page navigation happens. ----
    $('#btnFinalSubmit').on('click', function (e) {
        e.preventDefault();

        var $form = $(this).closest('form');
        var $btn = $(this);

        Swal.fire({
            icon: 'question',
            title: 'Submit Application?',
            text: 'Once submitted, you will not be able to edit the application details. Do you want to continue?',
            showCancelButton: true,
            confirmButtonText: 'Yes, Submit',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#1a3a52'
        }).then(function (result) {
            if (!result.isConfirmed) return;

            var originalText = $btn.text();
            $btn.prop('disabled', true).text('Submitting...');

            var formData = new FormData($form[0]);

            fetch('/TradeLicence/Apply', {
                method: 'POST',
                body: formData
            })
                .then(function (response) {
                    if (!response.ok) {
                        return response.text().then(function (text) {
                            var message = 'Submission failed. Please try again.';
                            try {
                                var data = JSON.parse(text);
                                if (data && data.errors) {
                                    var allMessages = [];
                                    Object.keys(data.errors).forEach(function (key) {
                                        (data.errors[key] || []).forEach(function (msg) {
                                            allMessages.push(msg);
                                        });
                                    });
                                    if (allMessages.length) message = allMessages.join('\n');
                                } else if (data && data.title) {
                                    message = data.title;
                                }
                            } catch (parseErr) {
                                // Response wasn't JSON (likely an unhandled server exception page).
                                console.error('Non-JSON error response:', text);
                            }
                            throw new Error(message);
                        });
                    }
                    return response.blob();
                })
                .then(function (blob) {
                    var url = window.URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.href = url;
                    a.download = 'Acknowledgement.pdf';
                    document.body.appendChild(a);
                    a.click();
                    a.remove();
                    window.URL.revokeObjectURL(url);

                    Swal.fire({
                        icon: 'success',
                        title: 'Application Submitted!',
                        text: 'Your acknowledgement slip has been downloaded.',
                        confirmButtonColor: '#1a3a52'
                    }).then(function () {
                        window.location.href = '/Dashboard/Status';
                    });
                })
                .catch(function (err) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Submission Failed',
                        text: err.message || 'Please try again.',
                        confirmButtonColor: '#1a3a52'
                    });
                })
                .finally(function () {
                    $btn.prop('disabled', false).text(originalText);
                });
        });
    });

});
