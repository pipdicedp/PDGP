/*
 * Industry officer — "Forward Application" on the CAF review page.
 *
 * Needs at least one department ticked, then asks for confirmation (listing the
 * chosen departments) before posting the form. Nothing is sent until the officer
 * confirms. No inline JavaScript in the view — everything is hooked by id/class.
 */
(function () {
    'use strict';

    var form = document.getElementById('cafForwardForm');
    var button = document.getElementById('cafForwardBtn');
    if (!form || !button) return;

    function esc(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function selectedDepartments() {
        return Array.prototype.map.call(
            form.querySelectorAll('.caf-fwd-check:checked'),
            function (el) { return el.value; }
        );
    }

    function send() {
        button.disabled = true; // no double submit
        form.submit();
    }

    button.addEventListener('click', function () {
        var departments = selectedDepartments();

        if (departments.length === 0) {
            if (window.Swal) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Select a department',
                    text: 'Please tick at least one department to forward this application to.',
                    confirmButtonColor: '#1a3a52'
                });
            } else {
                alert('Please select at least one department.');
            }
            return;
        }

        if (window.Swal) {
            Swal.fire({
                icon: 'question',
                title: 'Forward this application?',
                html: 'It will be sent <strong>only</strong> to:<br><br><strong>' +
                    departments.map(esc).join('<br>') + '</strong>',
                showCancelButton: true,
                confirmButtonText: 'Yes, Forward',
                cancelButtonText: 'Cancel',
                confirmButtonColor: '#1D6E3E',
                cancelButtonColor: '#6B7280',
                reverseButtons: true
            }).then(function (result) {
                if (result.isConfirmed) send();
            });
        } else if (window.confirm('Forward this application to: ' + departments.join(', ') + '?')) {
            send();
        }
    });
})();
