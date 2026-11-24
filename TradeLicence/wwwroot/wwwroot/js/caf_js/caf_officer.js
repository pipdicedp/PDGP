/*
 * Industry officer — the "Officer Workflow" actions on the CAF review page.
 *
 *  - Approve / Reject / Return to Applicant: ask for remarks (required for Reject and Return,
 *    because the applicant sees them), confirm, then post #cafDecisionForm.
 *  - Forward to department(s): reveals the department checkboxes. "Forward Application" needs at
 *    least one department ticked and asks for confirmation (listing them) before posting.
 *
 * No inline JavaScript in the view — everything is hooked by id / data attribute.
 */
(function () {
    'use strict';

    function esc(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    // ------------------------------------------------------------ Approve / Reject / Return
    var decisionForm = document.getElementById('cafDecisionForm');

    var ACTIONS = {
        approve: {
            title: 'Approve this application?',
            text: 'The applicant will see it as Approved. This is final.',
            confirm: 'Yes, Approve',
            color: '#1D6E3E',
            icon: 'question',
            remarksRequired: false,
            placeholder: 'Optional remarks...'
        },
        reject: {
            title: 'Reject this application?',
            text: 'The applicant will see it as Rejected, with your remarks. This is final.',
            confirm: 'Yes, Reject',
            color: '#B3261E',
            icon: 'warning',
            remarksRequired: true,
            placeholder: 'Reason for rejection (shown to the applicant)...'
        },
        'return': {
            title: 'Return to the applicant?',
            text: 'The applicant can edit the form and submit it again. Your remarks tell them what to correct.',
            confirm: 'Yes, Return',
            color: '#B26A00',
            icon: 'warning',
            remarksRequired: true,
            placeholder: 'What needs to be corrected (shown to the applicant)...'
        }
    };

    function postDecision(action, remarks) {
        document.getElementById('cafDecisionValue').value = action;
        document.getElementById('cafDecisionRemarks').value = remarks || '';
        decisionForm.submit();
    }

    if (decisionForm) {
        Array.prototype.forEach.call(document.querySelectorAll('[data-caf-action]'), function (btn) {
            btn.addEventListener('click', function () {
                if (btn.disabled) return;

                var action = btn.getAttribute('data-caf-action');
                var cfg = ACTIONS[action];
                if (!cfg) return;

                if (window.Swal) {
                    Swal.fire({
                        icon: cfg.icon,
                        title: cfg.title,
                        text: cfg.text,
                        input: 'textarea',
                        inputLabel: cfg.remarksRequired ? 'Remarks (required)' : 'Remarks (optional)',
                        inputPlaceholder: cfg.placeholder,
                        inputAttributes: { maxlength: 1000 },
                        showCancelButton: true,
                        confirmButtonText: cfg.confirm,
                        cancelButtonText: 'Cancel',
                        confirmButtonColor: cfg.color,
                        cancelButtonColor: '#6B7280',
                        reverseButtons: true,
                        preConfirm: function (value) {
                            var v = (value || '').trim();
                            if (cfg.remarksRequired && !v) {
                                Swal.showValidationMessage('Please enter remarks — the applicant will see them.');
                                return false;
                            }
                            return v;
                        }
                    }).then(function (result) {
                        if (result.isConfirmed) postDecision(action, result.value);
                    });
                } else {
                    var remarks = window.prompt(cfg.title + '\n' + (cfg.remarksRequired ? 'Remarks (required):' : 'Remarks (optional):'));
                    if (remarks === null) return;
                    if (cfg.remarksRequired && !remarks.trim()) { alert('Remarks are required.'); return; }
                    postDecision(action, remarks.trim());
                }
            });
        });
    }

    // ------------------------------------------------------------ Forward to department(s)
    var form = document.getElementById('cafForwardForm');
    var panel = document.getElementById('cafForwardPanel');
    var showBtn = document.getElementById('cafShowForward');
    var cancelBtn = document.getElementById('cafForwardCancel');
    var button = document.getElementById('cafForwardBtn');

    function openPanel() {
        if (!panel) return;
        panel.hidden = false;
        if (showBtn) showBtn.classList.add('caf-of-tile-active');
        panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function closePanel() {
        if (!panel) return;
        panel.hidden = true;
        if (showBtn) showBtn.classList.remove('caf-of-tile-active');
        if (form) {
            Array.prototype.forEach.call(form.querySelectorAll('.caf-fwd-check'), function (c) { c.checked = false; });
        }
    }

    if (showBtn) {
        showBtn.addEventListener('click', function () {
            if (showBtn.disabled) return;
            if (panel && panel.hidden) openPanel(); else closePanel();
        });
    }
    if (cancelBtn) cancelBtn.addEventListener('click', closePanel);

    function setAllDepartments(checked) {
        if (!form) return;
        Array.prototype.forEach.call(form.querySelectorAll('.caf-fwd-check'), function (c) { c.checked = checked; });
    }

    Array.prototype.forEach.call(document.querySelectorAll('[data-caf-select-all]'), function (b) {
        b.addEventListener('click', function () { setAllDepartments(true); });
    });
    Array.prototype.forEach.call(document.querySelectorAll('[data-caf-clear-all]'), function (b) {
        b.addEventListener('click', function () { setAllDepartments(false); });
    });

    if (!form || !button) return;

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
