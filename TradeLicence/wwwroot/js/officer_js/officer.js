// Fires the success/warning popup after a Forward / Approve / Return action
// redirects back to the dashboard. Called from Index.cshtml with the values
// read out of TempData there (Razor reads TempData, this just renders it).
function showOfficerActionAlert(actionType, message) {
    if (!message) return;

    var config = {
        forward: { icon: 'success', title: 'Forwarded!' },
        approve: { icon: 'success', title: 'Approved!' },
        revert: { icon: 'info', title: 'Reverted' },
        reject: { icon: 'error', title: 'Rejected' },
        return: { icon: 'warning', title: 'Returned to Applicant' },
        created: { icon: 'success', title: 'User Created!' },
        deleted: { icon: 'success', title: 'User Deleted' },
        toggled: { icon: 'success', title: 'Status Updated' },
        payment: { icon: 'success', title: 'Payment Updated' }
    }[actionType] || { icon: 'info', title: 'Done' };

    Swal.fire({
        icon: config.icon,
        title: config.title,
        text: message,
        confirmButtonColor: '#1a3a52'
    });
}

// The dropdown's value is "officerId:stage" (e.g. "7:Verification") —
// split it into the two hidden fields the controller actually reads.
var forwardForm = document.getElementById('forwardOfficerForm');
if (forwardForm) {
    forwardForm.addEventListener('submit', function (e) {
        var selected = document.getElementById('forwardOfficerSelect').value;
        var parts = selected.split(':');
        if (parts.length !== 2) {
            e.preventDefault();
            alert('Please select an officer to forward to.');
            return;
        }
        document.getElementById('forwardOfficerId').value = parts[0];
        document.getElementById('forwardTargetStage').value = parts[1];
    });
}

var btnApprove = document.getElementById('btnApprove');
if (btnApprove) {
    btnApprove.addEventListener('click', function () {
        Swal.fire({
            title: 'Approve Application',
            text: 'Optional remarks for the record.',
            input: 'textarea',
            inputPlaceholder: 'Remarks (optional)...',
            showCancelButton: true,
            confirmButtonText: 'Approve',
            confirmButtonColor: '#1D6E3E'
        }).then(function (result) {
            if (result.isConfirmed) {
                document.getElementById('approveRemarks').value = result.value || '';
                document.getElementById('approveForm').submit();
            }
        });
    });
}

var btnReject = document.getElementById('btnReject');
if (btnReject) {
    btnReject.addEventListener('click', function () {
        Swal.fire({
            title: 'Reject Application',
            text: 'This is final — the applicant cannot correct and resubmit a rejected application. Please state the reason.',
            input: 'textarea',
            inputPlaceholder: 'Reason for rejection (required)...',
            showCancelButton: true,
            confirmButtonText: 'Reject',
            confirmButtonColor: '#C0392B',
            inputValidator: function (value) {
                if (!value || !value.trim()) {
                    return 'Please enter a reason before rejecting the application.';
                }
            }
        }).then(function (result) {
            if (result.isConfirmed) {
                document.getElementById('rejectRemarks').value = result.value;
                document.getElementById('rejectForm').submit();
            }
        });
    });
}

var btnRevert = document.getElementById('btnRevert');
if (btnRevert) {
    btnRevert.addEventListener('click', function () {
        Swal.fire({
            title: 'Revert to Officer?',
            text: 'Optional remarks for why it\'s being sent back.',
            input: 'textarea',
            inputPlaceholder: 'Remarks (optional)...',
            showCancelButton: true,
            confirmButtonText: 'Revert',
            confirmButtonColor: '#D4A017'
        }).then(function (result) {
            if (result.isConfirmed) {
                document.getElementById('revertRemarks').value = result.value || '';
                document.getElementById('revertForm').submit();
            }
        });
    });
}

// ---------------- Inspection-stage payment ----------------
var paymentAccordionHeader = document.getElementById('paymentAccordionHeader');
if (paymentAccordionHeader) {
    var togglePaymentAccordion = function () {
        var body = document.getElementById('paymentAccordionBody');
        var isOpen = paymentAccordionHeader.getAttribute('aria-expanded') === 'true';
        var willOpen = !isOpen;

        paymentAccordionHeader.setAttribute('aria-expanded', String(willOpen));
        body.classList.toggle('tl-payment-body-open', willOpen);
        // Set directly too, not just via the class, so this doesn't depend
        // on dashboard.css having refreshed in the browser's cache.
        body.style.display = willOpen ? 'block' : 'none';
    };

    paymentAccordionHeader.addEventListener('click', togglePaymentAccordion);
    // It's a <div role="button"> now (not a real <legend>/<button>), so
    // Enter/Space need to be wired up manually for keyboard users.
    paymentAccordionHeader.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            togglePaymentAccordion();
        }
    });
}

var btnSendPayment = document.getElementById('btnSendPayment');
if (btnSendPayment) {
    btnSendPayment.addEventListener('click', function () {
        Swal.fire({
            icon: 'question',
            title: 'Send Payment Request?',
            text: 'The applicant will be asked to pay ₹1500 (₹1000 Application Payment + ₹500 Extra Charge) before this application can be forwarded for approval.',
            showCancelButton: true,
            confirmButtonText: 'Send Request',
            confirmButtonColor: '#1a3a52'
        }).then(function (result) {
            if (result.isConfirmed) {
                document.getElementById('sendPaymentForm').submit();
            }
        });
    });
}

// ---------------- Stage Supporting Document upload (Verification/Inspection) ----------------
// Submitted via fetch, not a normal form post: the page must stay exactly
// where it is (no redirect, no scroll-to-top), and this action has nothing
// to do with the Forward/Approve/Return popup wiring in showOfficerActionAlert
// above — that one is only for actions that redirect back to the dashboard.
var stageUploadForm = document.getElementById('stageDocumentUploadForm');
if (stageUploadForm) {
    stageUploadForm.addEventListener('submit', function (e) {
        e.preventDefault();

        var fileInput = stageUploadForm.querySelector('input[type="file"]');
        if (!fileInput.files.length) return;

        var formData = new FormData(stageUploadForm);
        var token = stageUploadForm.querySelector('input[name="__RequestVerificationToken"]').value;
        var submitBtn = stageUploadForm.querySelector('button[type="submit"]');
        submitBtn.disabled = true;

        fetch(stageUploadForm.action, {
            method: 'POST',
            headers: { 'RequestVerificationToken': token },
            body: formData
        })
            .then(function (response) {
                if (!response.ok) {
                    return response.json().then(function (data) {
                        throw new Error(data.error || 'Upload failed.');
                    });
                }
                return response.json();
            })
            .then(function (data) {
                var note = document.getElementById('currentStageFileNote');
                if (note) {
                    note.innerHTML = 'Current file: <strong>' + data.fileName + '</strong> (uploaded ' +
                        data.uploadedDate + '). Uploading a new file will replace it.';
                    note.style.display = '';
                }

                var previewBtn = document.getElementById('stageDocPreviewBtn');
                if (previewBtn) {
                    previewBtn.dataset.previewUrl = '/Officer/PreviewStageDocument?docId=' + data.docId;
                    previewBtn.dataset.contentType = data.contentType;
                    previewBtn.dataset.fileName = data.fileName;
                    previewBtn.style.display = '';
                }

                fileInput.value = '';
                // No popup here on purpose — the updated "Current file" note
                // and Preview button above are the feedback; a SweetAlert
                // modal isn't wanted for this action.
            })
            .catch(function (err) {
                Swal.fire({ icon: 'error', title: 'Upload failed', text: err.message, confirmButtonColor: '#1a3a52' });
            })
            .finally(function () {
                submitBtn.disabled = false;
            });
    });
}

// ---------------- Stage document preview (shows inline, doesn't download) ----------------
document.addEventListener('click', function (e) {
    var btn = e.target.closest('.tl-stage-preview-btn');
    if (!btn || !btn.dataset.previewUrl) return;

    var url = btn.dataset.previewUrl;
    var contentType = btn.dataset.contentType || '';
    var fileName = btn.dataset.fileName || 'document';
    var content = document.getElementById('stageDocPreviewContent');
    var title = document.getElementById('stageDocPreviewTitle');

    title.textContent = fileName;

    if (contentType.indexOf('image/') === 0) {
        content.innerHTML = '<img src="' + url + '" alt="' + fileName + '">';
    } else if (contentType === 'application/pdf') {
        content.innerHTML = '<iframe src="' + url + '"></iframe>';
    } else {
        content.innerHTML = '<p>This file type can\'t be previewed here. <a href="' + url + '" target="_blank">Open it in a new tab</a> instead.</p>';
    }

    document.getElementById('stageDocPreviewModal').style.display = 'flex';
});

var stageDocPreviewClose = document.getElementById('stageDocPreviewClose');
if (stageDocPreviewClose) {
    stageDocPreviewClose.addEventListener('click', closeStageDocPreview);
}
var stageDocPreviewModal = document.getElementById('stageDocPreviewModal');
if (stageDocPreviewModal) {
    // Click on the dark overlay (not the box itself) also closes it.
    stageDocPreviewModal.addEventListener('click', function (e) {
        if (e.target === stageDocPreviewModal) closeStageDocPreview();
    });
}
function closeStageDocPreview() {
    document.getElementById('stageDocPreviewModal').style.display = 'none';
    document.getElementById('stageDocPreviewContent').innerHTML = '';
}

// ---------------- Existing Officers page ----------------
// Both buttons live in a table with one row per officer, so — unlike the
// single buttons above — these are delegated on `document` rather than
// looked up by a single id. Each row already carries its own hidden
// #toggleForm-<id> / #deleteForm-<id>, so all these handlers do is confirm
// and then submit the right one.

document.addEventListener('click', function (e) {
    var btn = e.target.closest('.btn-status-toggle');
    if (!btn) return;

    var id = btn.dataset.officerId;
    var name = btn.dataset.officerName;
    var current = btn.dataset.currentStatus;
    var next = current === 'Active' ? 'Inactive' : 'Active';

    Swal.fire({
        icon: 'question',
        title: 'Change Status?',
        text: 'Set "' + name + '" to ' + next + '?',
        showCancelButton: true,
        confirmButtonText: 'Yes, ' + next,
        confirmButtonColor: '#1a3a52'
    }).then(function (result) {
        if (result.isConfirmed) {
            document.getElementById('toggleForm-' + id).submit();
        }
    });
});

document.addEventListener('click', function (e) {
    var btn = e.target.closest('.btn-delete-officer');
    if (!btn) return;

    var id = btn.dataset.officerId;
    var name = btn.dataset.officerName;

    Swal.fire({
        icon: 'warning',
        title: 'Delete this user?',
        text: 'This permanently deletes the officer account "' + name + '". This cannot be undone.',
        showCancelButton: true,
        confirmButtonText: 'Yes, Delete',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#B3261E'
    }).then(function (result) {
        if (result.isConfirmed) {
            document.getElementById('deleteForm-' + id).submit();
        }
    });
});