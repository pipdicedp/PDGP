/* Registration of Firms + Society Registration / Renewal - form behaviour.

   - Mandatory-field validation: every problem is marked next to its field (red border + message),
     listed in a summary box, and the user cannot submit until all are fixed.
   - Submit: validate -> SweetAlert confirm -> AJAX post -> SweetAlert "Application Submitted
     Successfully!" -> status tracking page (same flow as wwwroot/js/tl_js/tl_confirm_submit.js).
     The server re-validates everything and sends field errors back as JSON if anything is wrong.
   - Save as Draft is a normal post (only format checks, nothing is mandatory).

   Page markup contract (Views/FirmRegistration/Index.cshtml, Views/SocietyRegistration/Index.cshtml):
     form#regForm, #regErrorSummary
     fieldset#regPeople[data-prefix][data-noun] > .reg-people-add ([data-field] / [data-file] inputs)
     tbody#regPeopleBody[data-cols]  rows: tr[data-person-row] with [data-pf] inputs
     #regAddPerson, #regPeopleEmpty
     select#regAppType, [data-renewal-only], [data-registration-only], input#regExpiry, #regExpiryHint
     [data-submit-required] [data-label] [data-validate="mobile|email"] on mandatory inputs
     input[type=file][data-doc-required][data-has-existing]
     window.regServerErrors = { field: [messages] }   (set by the page after a non-AJAX post)
*/
(function () {
    'use strict';

    var form = document.getElementById('regForm');
    if (!form) return;

    var MAX_BYTES = 1048576; // 1 MB
    var summary = document.getElementById('regErrorSummary');
    var ERR = 'reg-field-error';

    // ------------------------------------------------------------------ helpers
    function warn(message) {
        if (window.Swal) {
            Swal.fire({ icon: 'warning', title: 'Please check', text: message, confirmButtonColor: '#1a3a52' });
        } else {
            alert(message);
        }
    }

    function esc(text) {
        var d = document.createElement('div');
        d.textContent = text;
        return d.innerHTML;
    }

    function extensionOf(file) {
        var parts = (file.name || '').split('.');
        return parts.length > 1 ? parts.pop().toLowerCase() : '';
    }

    function isVisible(el) { return el.offsetParent !== null; }

    var RULES = {
        mobile: { test: /^[6-9]\d{9}$/, message: 'Enter a valid 10-digit mobile number.' },
        email: { test: /^[^@\s]+@[^@\s]+\.[^@\s]+$/, message: 'Enter a valid email id.' }
    };

    // ------------------------------------------------------------------ inline errors
    function removeError(el) {
        if (!el) return;
        el.classList.remove('is-invalid');
        var anchor = el.type === 'checkbox' ? (el.closest('label') || el) : el;
        var next = anchor.nextElementSibling;
        if (next && next.classList.contains(ERR)) next.remove();
    }

    function showError(el, message) {
        if (!el) return;
        removeError(el);
        el.classList.add('is-invalid');
        var anchor = el.type === 'checkbox' ? (el.closest('label') || el) : el;
        var note = document.createElement('div');
        note.className = ERR;
        note.textContent = message;
        anchor.insertAdjacentElement('afterend', note);
    }

    function clearAllErrors() {
        var notes = form.querySelectorAll('.' + ERR);
        for (var i = 0; i < notes.length; i++) notes[i].remove();
        var bad = form.querySelectorAll('.is-invalid');
        for (var j = 0; j < bad.length; j++) bad[j].classList.remove('is-invalid');
        var rowsBad = form.querySelectorAll('.reg-row-invalid');
        for (var k = 0; k < rowsBad.length; k++) rowsBad[k].classList.remove('reg-row-invalid');
        if (summary) { summary.style.display = 'none'; summary.innerHTML = ''; }
    }

    // A field's error disappears as soon as the user fixes it.
    form.addEventListener('input', function (e) { if (e.target.classList.contains('is-invalid')) removeError(e.target); });
    form.addEventListener('change', function (e) { if (e.target.classList.contains('is-invalid')) removeError(e.target); });

    // problems: [{ el, message }]  (el may be null for a message without a field)
    function showProblems(problems, heading) {
        var seen = {};
        var items = [];
        problems.forEach(function (p) {
            if (p.el) showError(p.el, p.message);
            if (!seen[p.message]) { seen[p.message] = true; items.push(p.message); }
        });

        if (summary) {
            summary.innerHTML = '<strong>' + esc(heading) + '</strong><ul>' +
                items.map(function (m) { return '<li>' + esc(m) + '</li>'; }).join('') + '</ul>';
            summary.style.display = '';
        }

        var first = null;
        for (var i = 0; i < problems.length; i++) { if (problems[i].el) { first = problems[i].el; break; } }
        var target = first || summary;
        if (target && target.scrollIntoView) target.scrollIntoView({ behavior: 'smooth', block: 'center' });
        if (first && first.focus && first.type !== 'hidden') { try { first.focus({ preventScroll: true }); } catch (x) { /* ignore */ } }
        return items;
    }

    function popup(items, title) {
        if (!window.Swal) { alert(title + '\n- ' + items.join('\n- ')); return; }
        Swal.fire({
            icon: 'error',
            title: title,
            html: '<ul style="text-align:left; margin:0; padding-left:20px;">' +
                items.map(function (m) { return '<li>' + esc(m) + '</li>'; }).join('') + '</ul>',
            confirmButtonColor: '#1a3a52'
        });
    }

    // ------------------------------------------------------------------ file inputs (1 MB + type)
    form.addEventListener('change', function (e) {
        var input = e.target;
        if (!input || input.type !== 'file' || !input.files || !input.files.length) return;

        var file = input.files[0];
        var accept = (input.getAttribute('data-accept') || 'pdf').toLowerCase();
        var label = input.getAttribute('data-label') || 'File';

        if (file.size > MAX_BYTES) {
            input.value = '';
            showError(input, label + ': file size must be 1 MB or less.');
            return;
        }
        if (accept.split(',').indexOf(extensionOf(file)) === -1) {
            input.value = '';
            showError(input, label + ': only ' + accept.toUpperCase().replace(/,/g, ', ') + ' files are allowed.');
        }
    });

    // ------------------------------------------------------------------ people table
    var people = document.getElementById('regPeople');
    var body = document.getElementById('regPeopleBody');
    var addPanel = people ? people.querySelector('.reg-people-add') : null;
    var addBtn = document.getElementById('regAddPerson');
    var emptyNote = document.getElementById('regPeopleEmpty');
    var prefix = people ? people.getAttribute('data-prefix') : '';
    var noun = people ? (people.getAttribute('data-noun') || 'person') : 'person';
    var cols = body ? (body.getAttribute('data-cols') || '').split(',') : [];

    function rows() { return body ? body.querySelectorAll('tr[data-person-row]') : []; }

    function refreshTable() {
        var list = rows();
        for (var i = 0; i < list.length; i++) {
            var sl = list[i].querySelector('.reg-sl');
            if (sl) sl.textContent = i + 1;
        }
        if (emptyNote) emptyNote.style.display = list.length ? 'none' : '';
    }

    function hiddenInput(key, value) {
        var input = document.createElement('input');
        input.type = 'hidden';
        input.setAttribute('data-pf', key);
        input.value = value;
        return input;
    }

    function formatDate(iso) {
        var p = (iso || '').split('-');
        return p.length === 3 ? p[2] + '-' + p[1] + '-' + p[0] : iso;
    }

    function todayIso() {
        var d = new Date();
        var m = ('0' + (d.getMonth() + 1)).slice(-2), day = ('0' + d.getDate()).slice(-2);
        return d.getFullYear() + '-' + m + '-' + day;
    }

    function panelField(key) { return addPanel.querySelector('[data-field="' + key + '"]'); }

    function addPanelHasContent() {
        var fields = addPanel.querySelectorAll('[data-field], [data-file]');
        for (var i = 0; i < fields.length; i++) {
            var f = fields[i];
            if (f.type === 'file' ? (f.files && f.files.length) : f.value.trim()) return true;
        }
        return false;
    }

    function clearAddPanel() {
        var fields = addPanel.querySelectorAll('[data-field], [data-file]');
        for (var i = 0; i < fields.length; i++) {
            removeError(fields[i]);
            if (fields[i].hasAttribute('data-field')) fields[i].value = '';
        }
    }

    if (people && body && addPanel && addBtn) {

        addBtn.addEventListener('click', function () {
            var problems = [];
            var i, el;

            var fields = addPanel.querySelectorAll('[data-field]');
            for (i = 0; i < fields.length; i++) {
                el = fields[i];
                removeError(el);
                var label = el.getAttribute('data-label') || 'This field';
                if (el.hasAttribute('data-required') && !el.value.trim()) {
                    problems.push({ el: el, message: label + ' is required.' });
                } else if (el.type === 'date' && el.value && el.value > todayIso()) {
                    problems.push({ el: el, message: label + ' cannot be a future date.' });
                }
            }
            var fileInputs = addPanel.querySelectorAll('[data-file]');
            for (i = 0; i < fileInputs.length; i++) {
                el = fileInputs[i];
                removeError(el);
                if (el.hasAttribute('data-required') && !el.files.length) {
                    problems.push({ el: el, message: (el.getAttribute('data-label') || 'File') + ' is required.' });
                }
            }
            if (problems.length) {
                problems.forEach(function (p) { showError(p.el, p.message); });
                problems[0].el.focus();
                return;
            }

            var tr = document.createElement('tr');
            tr.setAttribute('data-person-row', '');

            var sl = document.createElement('td');
            sl.className = 'reg-sl';
            tr.appendChild(sl);

            cols.forEach(function (key, idx) {
                var source = panelField(key);
                var value = source ? source.value.trim() : '';
                var td = document.createElement('td');
                td.appendChild(document.createTextNode(source && source.type === 'date' ? formatDate(value) : value));
                td.appendChild(hiddenInput(key, value));
                if (idx === 0) td.appendChild(hiddenInput('Id', '0'));
                tr.appendChild(td);
            });

            // The real file input (holding the chosen file) moves into the row so it is posted with
            // the form; a fresh empty copy stays in the add panel for the next entry.
            for (i = 0; i < fileInputs.length; i++) {
                var chosen = fileInputs[i];
                var key = chosen.getAttribute('data-file');
                var td2 = document.createElement('td');

                var chip = document.createElement('span');
                chip.className = 'reg-file-chip';
                chip.textContent = chosen.files[0].name;
                td2.appendChild(chip);

                var fresh = chosen.cloneNode(false);
                fresh.value = '';
                chosen.parentNode.replaceChild(fresh, chosen);

                chosen.removeAttribute('data-file');
                chosen.removeAttribute('data-required');
                chosen.removeAttribute('id');
                chosen.className = '';
                chosen.setAttribute('data-pf', key);
                chosen.style.display = 'none';
                td2.appendChild(chosen);
                tr.appendChild(td2);
            }

            var actionTd = document.createElement('td');
            var del = document.createElement('button');
            del.type = 'button';
            del.className = 'reg-btn-del';
            del.setAttribute('data-reg-del', '');
            del.textContent = 'Delete';
            actionTd.appendChild(del);
            tr.appendChild(actionTd);

            body.appendChild(tr);
            clearAddPanel();
            refreshTable();

            // the "add at least one" error no longer applies
            var tableWrap = people.querySelector('.reg-table-wrap');
            if (tableWrap) removeError(tableWrap);
        });

        body.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-reg-del]');
            if (!btn) return;

            var remove = function () { btn.closest('tr').remove(); refreshTable(); };
            if (window.Swal) {
                Swal.fire({
                    icon: 'question',
                    title: 'Remove this ' + noun + '?',
                    showCancelButton: true,
                    confirmButtonText: 'Yes, remove',
                    confirmButtonColor: '#B3261E'
                }).then(function (r) { if (r.isConfirmed) remove(); });
            } else if (confirm('Remove this ' + noun + ' from the list?')) {
                remove();
            }
        });

        refreshTable();
    }

    // Row field names must be Partners[0].Name, Partners[1].Name ... with no gaps for model binding.
    function reindexRows() {
        var list = rows();
        for (var i = 0; i < list.length; i++) {
            var inputs = list[i].querySelectorAll('[data-pf]');
            for (var j = 0; j < inputs.length; j++) {
                inputs[j].name = prefix + '[' + i + '].' + inputs[j].getAttribute('data-pf');
            }
        }
    }

    // ------------------------------------------------------------------ society: Registration / Renewal
    var typeSelect = document.getElementById('regAppType');
    var expiry = document.getElementById('regExpiry');
    var expiryHint = document.getElementById('regExpiryHint');

    function applyType() {
        if (!typeSelect) return;
        var renewal = typeSelect.value === 'Renewal';
        var a = form.querySelectorAll('[data-renewal-only]');
        for (var i = 0; i < a.length; i++) a[i].style.display = renewal ? '' : 'none';
        var b = form.querySelectorAll('[data-registration-only]');
        for (var k = 0; k < b.length; k++) b[k].style.display = renewal ? 'none' : '';
    }

    function applyExpiryHint() {
        if (!expiry || !expiryHint) return;
        expiryHint.textContent = '';
        if (expiry.value && expiry.value < todayIso())
            expiryHint.textContent = 'Registration expired on ' + formatDate(expiry.value) + ' - eligible for renewal.';
    }

    if (typeSelect) { typeSelect.addEventListener('change', applyType); applyType(); }
    if (expiry) { expiry.addEventListener('change', applyExpiryHint); applyExpiryHint(); }

    // ------------------------------------------------------------------ validation
    // mode 'draft'  -> format checks only
    // mode 'submit' -> every mandatory field, document, row and the declaration
    function validate(mode) {
        var problems = [];
        var i, el;

        var inputs = form.querySelectorAll('[data-submit-required], [data-validate]');
        for (i = 0; i < inputs.length; i++) {
            el = inputs[i];
            if (!isVisible(el)) continue;               // e.g. renewal-only fields while registering
            var label = el.getAttribute('data-label') || 'This field';
            var value = (el.value || '').trim();

            if (!value) {
                if (mode === 'submit' && el.hasAttribute('data-submit-required'))
                    problems.push({ el: el, message: label + ' is required.' });
                continue;
            }
            var rule = RULES[el.getAttribute('data-validate')];
            if (rule && !rule.test.test(value)) problems.push({ el: el, message: rule.message });
        }

        if (mode !== 'submit') return problems;

        if (people) {
            var list = rows();
            var tableWrap = people.querySelector('.reg-table-wrap');
            if (list.length === 0) {
                problems.push({ el: tableWrap, message: 'Add at least one ' + noun + '.' });
            }
            // rows re-rendered after a server error that still need their files attached
            var pending = body.querySelectorAll('input[type=file][data-pf]');
            for (i = 0; i < pending.length; i++) {
                if (!pending[i].files.length) {
                    pending[i].closest('tr').classList.add('reg-row-invalid');
                    problems.push({ el: null, message: 'Attach the ID proof and signature again for the ' + noun + 's marked in the table.' });
                }
            }
        }

        var docs = form.querySelectorAll('input[type=file][data-doc-required]');
        for (i = 0; i < docs.length; i++) {
            var hasFile = docs[i].files && docs[i].files.length;
            if (!hasFile && docs[i].getAttribute('data-has-existing') !== 'true')
                problems.push({ el: docs[i], message: (docs[i].getAttribute('data-label') || 'Document') + ' is required.' });
        }

        var decl = document.getElementById('regDeclaration');
        if (decl && !decl.checked) problems.push({ el: decl, message: 'Please accept the declaration.' });

        return problems;
    }

    // ------------------------------------------------------------------ server errors -> inline
    function showServerErrors(map) {
        var problems = [];
        Object.keys(map || {}).forEach(function (key) {
            (map[key] || []).forEach(function (message) {
                var el = null;
                if (key) {
                    try { el = form.querySelector('[name="' + key + '"]'); } catch (x) { el = null; }
                    var m = key.match(/^(Partners|Members)\[(\d+)\]/);
                    if (m && body) {
                        var row = body.querySelectorAll('tr[data-person-row]')[parseInt(m[2], 10)];
                        if (row) row.classList.add('reg-row-invalid');
                    }
                }
                problems.push({ el: el, message: message });
            });
        });
        return problems.length ? showProblems(problems, 'Please correct the following:') : [];
    }

    if (window.regServerErrors && Object.keys(window.regServerErrors).length) {
        showServerErrors(window.regServerErrors);
    }

    // ------------------------------------------------------------------ submit
    function ajaxSubmit(button) {
        reindexRows();
        var data = new FormData(form);
        data.set('actionButton', 'submit');

        var originalText = button.textContent;
        button.disabled = true;
        button.textContent = 'Submitting...';

        fetch(form.getAttribute('action'), {
            method: 'POST',
            body: data,
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (response) {
                return response.json().catch(function () { return null; }).then(function (json) {
                    return { ok: response.ok, status: response.status, json: json };
                });
            })
            .then(function (res) {
                if (res.ok && res.json && res.json.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Application Submitted Successfully!',
                        text: res.json.applicationNumber ? 'Your application number is ' + res.json.applicationNumber + '.' : '',
                        confirmButtonColor: '#1a3a52',
                        allowOutsideClick: false
                    }).then(function () {
                        window.location.href = res.json.redirectUrl || '/';
                    });
                    return;
                }

                if (res.json && res.json.errors) {
                    clearAllErrors();
                    var items = showServerErrors(res.json.errors);
                    popup(items.length ? items : ['Submission failed. Please try again.'], 'Submission Failed');
                } else if (res.status === 400 || res.status === 403) {
                    popup(['Your session may have expired. Please refresh the page and try again.'], 'Submission Failed');
                } else {
                    popup(['Submission failed. Please try again.'], 'Submission Failed');
                }
            })
            .catch(function () {
                popup(['Could not reach the server. Please check your connection and try again.'], 'Submission Failed');
            })
            .then(function () {
                button.disabled = false;
                button.textContent = originalText;
            });
    }

    form.addEventListener('submit', function (e) {
        var submitter = e.submitter;
        var isSubmit = submitter && submitter.value === 'submit';

        clearAllErrors();

        if (people && addPanel && addPanelHasContent()) {
            e.preventDefault();
            var note = 'You have filled in a ' + noun + ' but not added it. Click "Add" first, or clear those fields.';
            showProblems([{ el: addBtn, message: note }], 'Please correct the following:');
            popup([note], 'Unsaved ' + noun);
            return;
        }

        var problems = validate(isSubmit ? 'submit' : 'draft');
        if (problems.length) {
            e.preventDefault();
            var items = showProblems(problems, isSubmit
                ? 'The following mandatory fields are missing or invalid:'
                : 'Please correct the following:');
            popup(items, isSubmit ? 'Mandatory fields missing' : 'Please correct the details');
            return;
        }

        if (!isSubmit) {                 // Save as Draft: normal post
            reindexRows();
            return;
        }

        e.preventDefault();              // Submit: confirm, then AJAX
        Swal.fire({
            icon: 'question',
            title: 'Submit Application?',
            text: 'Once submitted, you will not be able to edit the application details. Do you want to continue?',
            showCancelButton: true,
            confirmButtonText: 'Yes, Submit',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#1a3a52'
        }).then(function (result) {
            if (result.isConfirmed) ajaxSubmit(submitter);
        });
    });
})();
