/*
 * Common Application Form — FIELD VALIDATION
 * ==========================================================================
 * Edit the three tables below to change any rule. Nothing else needs touching.
 *
 *   1. RULE_TYPES    what a kind of field accepts (allowed characters, pattern, message)
 *   2. FIELD_RULES   which form field uses which rule type   (key = the field's name)
 *   3. REQUIRED      fields that cannot be left empty
 *
 * Behaviour
 *   - Characters a field does not allow are removed AS THE USER TYPES OR PASTES
 *     (e.g. letters never appear in a phone number, digits never appear in a name).
 *   - On leaving a field, a red message appears under it if the value is invalid.
 *   - "Save & Next" is blocked until every field is valid; the first bad field is focused.
 *   - The "+ Add" button of the sub-tables checks its row the same way.
 *
 * To add a rule for a new field: add one line to FIELD_RULES, e.g.  gstno: 'gst'
 * (and, if 'gst' is a new kind of field, one entry in RULE_TYPES).
 *
 * NOTE: this runs in the browser only. It is a convenience for the applicant, not a
 * security check — the server must still validate anything that really matters.
 */
(function () {
    'use strict';

    // ---------------------------------------------------------------------
    // 1. RULE TYPES
    //    strip   : characters matching this are removed while typing
    //    max     : maximum length
    //    pattern : the finished value must match this
    //    upper / lower : force the case
    //    message : shown under the field when 'pattern' fails
    // ---------------------------------------------------------------------
    var RULE_TYPES = {
        // Person / place names: letters (any language), spaces, dot, apostrophe, hyphen. NO digits.
        personName: {
            strip: /[^\p{L}\p{M} .'\-]/gu, max: 50,
            pattern: /^[\p{L}\p{M}][\p{L}\p{M} .'\-]*$/u,
            message: 'Enter a valid name. Only letters are allowed (no numbers or special characters).'
        },
        placeName: {
            strip: /[^\p{L}\p{M} .'\-]/gu, max: 50,
            pattern: /^[\p{L}\p{M}][\p{L}\p{M} .'\-]*$/u,
            message: 'Only letters are allowed here (no numbers or special characters).'
        },
        // Designation: letters plus & , / ( ) . -
        title: {
            strip: /[^\p{L}\p{M} .,&'()\/\-]/gu, max: 50,
            pattern: /^[\p{L}\p{M}][\p{L}\p{M} .,&'()\/\-]*$/u,
            message: 'Enter a valid designation. Only letters are allowed (no numbers).'
        },
        // Industry / unit name: letters, digits and . , & ( ) ' / -  (must contain a letter)
        orgName: {
            strip: /[^\p{L}\p{M}0-9 .,&'()\/\-]/gu, max: 50,
            pattern: /^(?=.*\p{L})[\p{L}\p{M}0-9 .,&'()\/\-]+$/u,
            message: 'Enter a valid name. Letters and numbers are allowed, and it must contain at least one letter.'
        },
        // Address lines: letters, digits and the usual address punctuation
        address: {
            strip: /[^\p{L}\p{M}0-9 .,#&'()\/:\-]/gu,
            pattern: /^[\p{L}\p{M}0-9 .,#&'()\/:\-]+$/u,
            message: 'Address contains characters that are not allowed.'
        },
        // Survey / plot / document numbers such as 123/4A
        refNo: {
            strip: /[^A-Za-z0-9 ,\/\-]/g, upper: true,
            pattern: /^[A-Za-z0-9][A-Za-z0-9 ,\/\-]*$/,
            message: 'Use only letters, numbers, / and - here.'
        },
        // 10-digit Indian mobile number, starting 6-9
        mobile: {
            numeric: true,
            strip: /\D/g, max: 10,
            pattern: /^[6-9]\d{9}$/,
            message: 'Enter a valid 10-digit mobile number (it must start with 6, 7, 8 or 9).'
        },
        // Landline: STD code + number, digits only (8 to 11 digits)
        landline: {
            numeric: true,
            strip: /\D/g, max: 11,
            pattern: /^\d{8,11}$/,
            message: 'Enter digits only, with the STD code (8 to 11 digits).'
        },
        fax: {
            numeric: true,
            strip: /\D/g, max: 11,
            pattern: /^\d{8,11}$/,
            message: 'Fax number must contain digits only (8 to 11 digits).'
        },
        // 6-digit PIN code, cannot start with 0
        pincode: {
            numeric: true,
            strip: /\D/g, max: 6,
            pattern: /^[1-9]\d{5}$/,
            message: 'Enter a valid 6-digit pincode.'
        },
        // 12-digit Aadhaar, cannot start with 0 or 1
        aadhaar: {
            numeric: true,
            strip: /\D/g, max: 12,
            pattern: /^[2-9]\d{11}$/,
            message: 'Enter a valid 12-digit Aadhaar number.'
        },
        // PAN: 5 letters + 4 digits + 1 letter  (e.g. ABCDE1234F)
        pan: {
            strip: /[^A-Za-z0-9]/g, max: 10, upper: true,
            pattern: /^[A-Z]{5}\d{4}[A-Z]$/,
            message: 'Enter a valid PAN, like ABCDE1234F (5 letters, 4 digits, 1 letter).'
        },
        // Udyam / UAM registration number (letters, digits, hyphen)
        udyam: {
            strip: /[^A-Za-z0-9\-]/g, max: 25, upper: true,
            pattern: /^[A-Z0-9\-]{12,25}$/,
            message: 'Enter a valid Udyam / UAM number, like UDYAM-PY-01-0001234.'
        },
        email: {
            strip: /\s/g, max: 50, lower: true,
            pattern: /^[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}$/,
            message: 'Enter a valid email address, like name@example.com.'
        },
        // Whole number, 0 or more
        integer: {
            numeric: true, strip: /\D/g, max: 10,
            pattern: /^\d{1,10}$/, maxValue: 2147483647,
            message: 'Enter a whole number (digits only).'
        },
        // Number with decimals, 0 or more. Number of decimals comes from the field's step (0.01 = 2).
        decimal: { message: 'Enter a valid number (digits and one decimal point only).' },
        // Anything else: only block characters that could break the page
        text: { strip: /[<>]/g }
    };

    // ---------------------------------------------------------------------
    // 2. WHICH FIELD USES WHICH RULE   (key = field name on the form)
    //    Number / decimal fields that are not listed here are handled
    //    automatically (digits only, 0 or more). Unlisted text fields use 'text'.
    // ---------------------------------------------------------------------
    var FIELD_RULES = {
        // Unit / applicant
        indname: 'orgName',
        promotername: 'personName',
        authpersonname: 'personName',
        designation: 'title',
        nactivityname: 'orgName',
        catofapp: 'placeName',
        category: 'placeName',

        // Communication / promoter address
        CommunicationAddress1: 'address', CommunicationAddress2: 'address', CommunicationAddress3: 'address',
        CommunicationState: 'placeName', CommunicationDistrict: 'placeName', pincode: 'pincode',
        PromotorAddress1: 'address', PromotorAddress2: 'address', PromotorAddress3: 'address',
        PromotorState: 'placeName', PromotorDistrict: 'placeName', PromotorPinCode: 'pincode',

        // Identification & contact
        aadharno: 'aadhaar',
        uam: 'udyam',
        pannumber: 'pan',
        officeno: 'landline',
        residenceno: 'landline',
        mobileno: 'mobile',
        altmobileno: 'mobile',
        fax: 'fax',
        email: 'email',

        // Site details
        reason: 'address',
        resurveyno: 'refNo',
        ucountry: 'placeName', ustate: 'placeName', udistrict: 'placeName',
        sitecommune: 'placeName', sitevillagename: 'placeName',
        sitepincode: 'pincode',
        siteaddress: 'address',
        BuildingType: 'placeName'
    };

    // ---------------------------------------------------------------------
    // 3. REQUIRED FIELDS (cannot be left empty). Remove a name to make it optional.
    // ---------------------------------------------------------------------
    var REQUIRED = ['indname', 'promotername', 'authpersonname', 'mobileno', 'email'];

    // ======================================================================
    // Engine — normally no need to edit below this line
    // ======================================================================

    function fieldKey(el) { return el.getAttribute('name') || el.getAttribute('data-field') || ''; }

    // Look at the element ONCE and remember what kind of field it is.
    function describe(el) {
        if (el._cafRule) return el._cafRule;

        var key = fieldKey(el);
        var ruleName = FIELD_RULES[key];
        var info = { key: key, required: REQUIRED.indexOf(key) !== -1 };

        if (el.tagName === 'SELECT') {
            info.kind = 'select';
        } else if (ruleName && RULE_TYPES[ruleName]) {
            info.kind = 'rule';
            info.rule = RULE_TYPES[ruleName];
        } else if (el.type === 'number') {
            var step = (el.getAttribute('step') || '1').toLowerCase();
            var decimals = 0;
            if (step === 'any') decimals = 2;
            else if (step.indexOf('.') !== -1) decimals = step.split('.')[1].length;
            info.kind = decimals > 0 ? 'decimal' : 'integer';
            info.decimals = decimals;
            info.rule = RULE_TYPES[info.kind];
        } else if (el.tagName === 'TEXTAREA' || el.type === 'text' || el.type === 'email') {
            info.kind = 'rule';
            info.rule = RULE_TYPES.text;
        } else {
            info.kind = 'none';
        }

        el._cafRule = info;
        return info;
    }

    // Number boxes become plain text boxes so letters/maxlength can be controlled.
    function prepare(el) {
        var info = describe(el);
        if (el._cafPrepared) return;
        el._cafPrepared = true;

        if (el.type === 'number') {
            el.type = 'text';
            el.removeAttribute('step');
            el.removeAttribute('min');
            el.removeAttribute('max');
        }
        if (el.type === 'email') {
            el.type = 'text';                       // our own check replaces the browser's
            el.setAttribute('inputmode', 'email');
        } else if (info.kind === 'decimal') {
            el.setAttribute('inputmode', 'decimal');
        } else if (info.rule && info.rule.numeric) {
            el.setAttribute('inputmode', 'numeric'); // number keypad on phones
        }
        if (info.rule && info.rule.max && !el.getAttribute('maxlength')) {
            el.setAttribute('maxlength', info.rule.max);
        }
        if (info.rule && info.rule.max && Number(el.getAttribute('maxlength')) > info.rule.max) {
            el.setAttribute('maxlength', info.rule.max);
        }
        el.setAttribute('autocomplete', 'off');
    }

    // Remove characters the field does not allow. Returns the cleaned text.
    function clean(el, text) {
        var info = describe(el);
        if (info.kind === 'select' || info.kind === 'none') return text;

        if (info.kind === 'decimal') {
            text = text.replace(/[^\d.]/g, '');
            var firstDot = text.indexOf('.');
            if (firstDot !== -1) {
                text = text.slice(0, firstDot + 1) + text.slice(firstDot + 1).replace(/\./g, '');
                text = text.slice(0, firstDot + 1 + info.decimals);
            }
            // at most 13 digits before the decimal point (fits numeric(18,2))
            var parts = text.split('.');
            if (parts[0].length > 13) parts[0] = parts[0].slice(0, 13);
            return parts.join('.');
        }

        var rule = info.rule;
        if (!rule) return text;
        if (rule.strip) text = text.replace(rule.strip, '');
        if (rule.upper) text = text.toUpperCase();
        if (rule.lower) text = text.toLowerCase();
        if (rule.max && text.length > rule.max) text = text.slice(0, rule.max);
        return text;
    }

    // Returns '' if fine, otherwise the message to show.
    function check(el) {
        var info = describe(el);
        if (info.kind === 'none') return '';

        var value = (el.value || '').trim();
        if (value === '') return info.required ? 'This field is required.' : '';

        if (info.kind === 'select') return '';

        if (info.kind === 'decimal') {
            if (!/^(\d+(\.\d*)?|\.\d+)$/.test(value)) return RULE_TYPES.decimal.message;
            return '';
        }

        var rule = info.rule;
        if (rule && rule.pattern && !rule.pattern.test(value)) return rule.message;
        if (rule && rule.maxValue && Number(value) > rule.maxValue) return 'That number is too large.';
        return '';
    }

    // ---- error display ----
    function errorBox(el) {
        var box = el._cafErrBox;
        if (box) return box;
        box = document.createElement('div');
        box.className = 'caf-field-error';
        box.setAttribute('role', 'alert');
        box.style.display = 'none';
        el.insertAdjacentElement('afterend', box);
        el._cafErrBox = box;
        return box;
    }

    function setError(el, message) {
        var box = errorBox(el);
        if (message) {
            box.textContent = message;
            box.style.display = '';
            el.classList.add('caf-invalid');
            el.setAttribute('aria-invalid', 'true');
        } else {
            box.textContent = '';
            box.style.display = 'none';
            el.classList.remove('caf-invalid');
            el.removeAttribute('aria-invalid');
        }
    }

    function validateOne(el) {
        var message = check(el);

        // Alternate mobile must be different from the main mobile number.
        if (!message && fieldKey(el) === 'altmobileno' && el.value) {
            var main = document.querySelector('[name="mobileno"]');
            if (main && main.value && main.value === el.value)
                message = 'Alternate mobile number must be different from the mobile number.';
        }

        setError(el, message);
        return message === '';
    }

    function attach(el) {
        if (el._cafAttached) return;
        var info = describe(el);
        if (info.kind === 'none') return;
        el._cafAttached = true;
        prepare(el);

        if (info.kind !== 'select') {
            // typing / pasting: strip what is not allowed, keep the caret where it was
            el.addEventListener('input', function () {
                var before = el.value;
                var after = clean(el, before);
                if (after !== before) {
                    var pos = el.selectionStart;
                    var removed = before.length - after.length;
                    el.value = after;
                    try { el.setSelectionRange(Math.max(0, pos - removed), Math.max(0, pos - removed)); } catch (e) { }
                }
                if (el.classList.contains('caf-invalid')) validateOne(el); // clear the message as soon as it is fixed
            });
        }
        el.addEventListener('blur', function () {
            // tidy up: trim, and drop a trailing "." on decimals ("12." -> "12")
            var v = el.value.trim();
            if (info.kind === 'decimal') v = v.replace(/\.$/, '');
            if (v !== el.value) el.value = v;
            validateOne(el);
        });
        el.addEventListener('change', function () { validateOne(el); });
    }

    function controlsIn(root) {
        return Array.prototype.slice.call(root.querySelectorAll(
            'input:not([type=hidden]):not([type=file]):not([type=submit]):not([type=button]), select, textarea'))
            .filter(function (el) { return !el.disabled; });
    }

    // Check a list of controls; show every error; focus the first bad one.
    function validateAll(list) {
        var firstBad = null, bad = 0;
        list.forEach(function (el) {
            if (!validateOne(el)) { bad++; if (!firstBad) firstBad = el; }
        });
        if (firstBad) {
            try { firstBad.scrollIntoView({ behavior: 'smooth', block: 'center' }); } catch (e) { }
            firstBad.focus();
        }
        return { ok: bad === 0, count: bad };
    }

    function alertInvalid(count) {
        var text = count === 1
            ? 'One field needs to be corrected. It is marked in red.'
            : count + ' fields need to be corrected. They are marked in red.';
        if (window.Swal) Swal.fire({ icon: 'warning', title: 'Please check the form', text: text, confirmButtonColor: '#1a3a52' });
        else alert(text);
    }

    // ---- wire up the page ----
    var stepForm = document.getElementById('cafStepForm');
    if (stepForm) {
        var mainControls = controlsIn(stepForm).filter(function (el) { return !el.classList.contains('caf-sub-input'); });
        mainControls.forEach(attach);

        // sub-table entry boxes (outside the form's own save)
        controlsIn(stepForm).filter(function (el) { return el.classList.contains('caf-sub-input'); }).forEach(attach);

        stepForm.addEventListener('submit', function (e) {
            var result = validateAll(mainControls);
            if (!result.ok) {
                e.preventDefault();
                alertInvalid(result.count);
            }
        });
    }

    // Used by the sub-table "+ Add" buttons further down this file.
    window.CafValidation = {
        validateAll: validateAll,
        alertInvalid: alertInvalid
    };
})();

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

            // field rules (see FIELD VALIDATION at the top of this file)
            if (window.CafValidation && !window.CafValidation.validateAll(Array.prototype.slice.call(inputs)).ok) {
                showError('Please correct the fields marked in red before adding.');
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
