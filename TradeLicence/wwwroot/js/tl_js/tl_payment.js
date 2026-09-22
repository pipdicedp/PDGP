// Delegated on document, not a single button id — the same .btn-pay-now
// button can appear on multiple rows across Index.cshtml and Status.cshtml,
// so this works no matter how many are on the page or which page it is.
document.addEventListener('click', function (e) {
    var btn = e.target.closest('.btn-pay-now');
    if (!btn) return;

    var form = btn.closest('form');
    if (!form) return;

    Swal.fire({
        icon: 'question',
        title: 'Confirm Payment',
        text: 'TEMPORARY test action — this simulates a successful payment. Proceed?',
        showCancelButton: true,
        confirmButtonText: 'Pay Now',
        confirmButtonColor: '#1D6E3E'
    }).then(function (result) {
        if (result.isConfirmed) {
            form.submit();
        }
    });
});
