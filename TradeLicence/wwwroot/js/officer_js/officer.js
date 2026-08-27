// Fires the success/warning popup after a Forward / Approve / Return action
// redirects back to the dashboard. Called from Index.cshtml with the values
// read out of TempData there (Razor reads TempData, this just renders it).
function showOfficerActionAlert(actionType, message) {
    if (!message) return;

    var config = {
        forward: { icon: 'success', title: 'Forwarded!' },
        approve: { icon: 'success', title: 'Approved!' },
        revert: { icon: 'info', title: 'Reverted' },
        return: { icon: 'warning', title: 'Returned to Applicant' },
        created: { icon: 'success', title: 'User Created!' },
        deleted: { icon: 'success', title: 'User Deleted' },
        toggled: { icon: 'success', title: 'Status Updated' }
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

var btnRevert = document.getElementById('btnRevert');
if (btnRevert) {
    btnRevert.addEventListener('click', function () {
        var revertTo = this.dataset.revertTo;
        Swal.fire({
            title: 'Revert to ' + revertTo + '?',
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