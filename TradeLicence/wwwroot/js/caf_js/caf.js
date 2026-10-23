/*
 * Common Application Form — "Add" button for each sub-table.
 *
 * Clicking "+ Add" on a sub-table immediately POSTs that row to the server
 * (dbo.caf_*_sub_table*) and, on success, appends it to the mini-table below
 * — it is NOT just held in the browser until a later bulk "Save". Clicking
 * "Delete" on a row removes it from the database the same way.
 *
 * The main step fields (Name of Industry, Land Cost, etc.) are an ordinary
 * <form method="post">, submitted normally by "Save & Next" — no JS needed
 * for those.
 */
(function () {
    'use strict';

    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenInput ? tokenInput.value : '';

    function confirmDelete(onConfirm) {
        if (window.Swal) {
            Swal.fire({
                icon: 'warning',
                title: 'Delete this row?',
                text: 'This cannot be undone.',
                showCancelButton: true,
                confirmButtonText: 'Yes, delete',
                cancelButtonText: 'Cancel',
                confirmButtonColor: '#B3261E'
            }).then(function (result) {
                if (result.isConfirmed) onConfirm();
            });
        } else if (window.confirm('Delete this row? This cannot be undone.')) {
            onConfirm();
        }
    }

    function showError(message) {
        if (window.Swal) {
            Swal.fire({ icon: 'error', title: 'Could not save', text: message, confirmButtonColor: '#1a3a52' });
        } else {
            alert(message);
        }
    }

    function post(url, params) {
        params.append('__RequestVerificationToken', token);
        return fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: params
        }).then(function (res) {
            return res.json().catch(function () { return {}; }).then(function (data) {
                if (!res.ok) throw new Error(data.error || 'Request failed.');
                return data;
            });
        });
    }

    Array.prototype.forEach.call(document.querySelectorAll('.caf-subtable'), function (fieldset) {
        var step = fieldset.getAttribute('data-step');
        var subKey = fieldset.getAttribute('data-subkey');
        var inputs = fieldset.querySelectorAll('.caf-sub-input');
        var body = fieldset.querySelector('.caf-subtable-body');
        var emptyNote = fieldset.querySelector('.caf-subtable-empty');
        var addBtn = fieldset.querySelector('.caf-add-row');

        function currentValues() {
            var values = {};
            inputs.forEach(function (el) { values[el.getAttribute('data-field')] = el.value; });
            return values;
        }

        function hasAnyValue(values) {
            return Object.keys(values).some(function (k) { return (values[k] || '').trim() !== ''; });
        }

        function appendRow(id, values) {
            var tr = document.createElement('tr');
            tr.setAttribute('data-id', id);

            inputs.forEach(function (el) {
                var td = document.createElement('td');
                td.textContent = values[el.getAttribute('data-field')] || '';
                tr.appendChild(td);
            });

            var actionTd = document.createElement('td');
            actionTd.className = 'caf-col-action';
            var delBtn = document.createElement('button');
            delBtn.type = 'button';
            delBtn.className = 'btn btn-danger btn-sm caf-delete-row';
            delBtn.textContent = 'Delete';
            actionTd.appendChild(delBtn);
            tr.appendChild(actionTd);

            body.appendChild(tr);
            if (emptyNote) emptyNote.style.display = 'none';
        }

        addBtn.addEventListener('click', function () {
            var values = currentValues();
            if (!hasAnyValue(values)) {
                showError('Please fill in at least one field before adding.');
                return;
            }

            var params = new URLSearchParams();
            params.append('step', step);
            params.append('subKey', subKey);
            Object.keys(values).forEach(function (k) { params.append(k, values[k] || ''); });

            addBtn.disabled = true;
            post('/common_application_form/subrow/add', params)
                .then(function (data) {
                    appendRow(data.id, data.values || values);
                    inputs.forEach(function (el) { el.value = ''; });
                })
                .catch(function (err) { showError(err.message); })
                .finally(function () { addBtn.disabled = false; });
        });

        body.addEventListener('click', function (e) {
            var btn = e.target.closest('.caf-delete-row');
            if (!btn) return;

            var row = btn.closest('tr');
            var id = row.getAttribute('data-id');

            confirmDelete(function () {
                var params = new URLSearchParams();
                params.append('step', step);
                params.append('subKey', subKey);
                params.append('id', id);

                post('/common_application_form/subrow/delete', params)
                    .then(function () {
                        row.remove();
                        if (!body.querySelector('tr') && emptyNote) emptyNote.style.display = '';
                    })
                    .catch(function (err) { showError(err.message); });
            });
        });
    });

    // ------------------------------------------------------------------
    // Application Preview — "Submit Application" button.
    // (caf.js is also loaded on the data-entry steps, so everything is
    //  null-guarded: on those pages these elements simply don't exist.)
    // ------------------------------------------------------------------
    var previewPage = document.getElementById('cafPreviewPage');
    var submitForm = document.getElementById('cafSubmitForm');
    var submitBtn = document.getElementById('cafSubmitBtn');

    // After a successful submit the server redirects back to the preview with data-just-submitted="true".
    if (previewPage && previewPage.getAttribute('data-just-submitted') === 'true') {
        var statusUrl = previewPage.getAttribute('data-status-url');
        if (window.Swal) {
            Swal.fire({
                icon: 'success',
                title: 'Application submitted',
                text: 'Your application has been sent to the Industry Department.',
                confirmButtonText: 'Track Status',
                confirmButtonColor: '#1D6E3E',
                allowOutsideClick: false
            }).then(function () { window.location.href = statusUrl; });
        } else {
            alert('Application submitted.');
            window.location.href = statusUrl;
        }
    }

    if (submitForm && submitBtn) {
        var sendSubmit = function () {
            submitBtn.disabled = true; // no double submit
            submitForm.submit();
        };

        submitBtn.addEventListener('click', function () {
            if (submitBtn.disabled) return;

            if (window.Swal) {
                Swal.fire({
                    icon: 'question',
                    title: 'Submit this application?',
                    text: 'Once submitted it cannot be edited unless the Industry Department returns it.',
                    showCancelButton: true,
                    confirmButtonText: 'Yes, Submit',
                    cancelButtonText: 'Cancel',
                    confirmButtonColor: '#1D6E3E',
                    cancelButtonColor: '#6B7280',
                    reverseButtons: true
                }).then(function (result) {
                    if (result.isConfirmed) sendSubmit();
                });
            } else if (window.confirm('Submit this application? It cannot be edited afterwards.')) {
                sendSubmit();
            }
        });
    }

    /* ---------- Step 6: quick client-side check of chosen documents ----------
   The server re-checks everything (size, extension, and the file's real
   type); this only saves the applicant a round trip. */
    (function () {
        var MAX_BYTES = 5 * 1024 * 1024;
        var OK_EXT = /\.(pdf|jpe?g|png)$/i;

        document.querySelectorAll('.caf-file-input').forEach(function (input) {
            input.addEventListener('change', function () {
                var file = input.files && input.files[0];
                if (!file) return;

                var problem = null;
                if (!OK_EXT.test(file.name)) problem = 'Only PDF, JPG or PNG files are accepted.';
                else if (file.size > MAX_BYTES) problem = 'This file is larger than 5 MB.';

                if (problem) {
                    input.value = '';
                    if (window.Swal) Swal.fire({ icon: 'warning', title: 'Cannot use this file', text: problem });
                    else alert(problem);
                }
            });
        });
    })();

})();
