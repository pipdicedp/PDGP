// Fires the success/warning popup after a Forward / Approve / Return action
// redirects back to the dashboard. Called from Index.cshtml with the values
// read out of TempData there (Razor reads TempData, this just renders it).
function showOfficerActionAlert(actionType, message) {
    if (!message) return;

    var config = {
        forward: { icon: 'success', title: 'Forwarded!' },
        approve: { icon: 'success', title: 'Approved!' },
        return: { icon: 'warning', title: 'Returned to Applicant' }
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