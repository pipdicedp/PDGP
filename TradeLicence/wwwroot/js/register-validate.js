/* Registration popup: live validation (PAN, mobile, DOB, email, password strength).
   The server still validates everything again when the form is submitted. */
(function () {
    'use strict';

    const modal = document.getElementById('registerModal');
    if (!modal) return;
    const form = modal.querySelector('form');
    if (!form) return;

    // Our own validation replaces the jQuery unobtrusive rules for this form only.
    // (This script runs before jQuery parses the page, so removing data-val is enough.)
    form.querySelectorAll('[data-val]').forEach(function (i) { i.removeAttribute('data-val'); });

    const $ = function (id) { return document.getElementById(id); };
    const PAN_TYPES = {
        P: 'Individual', C: 'Company', H: 'Hindu Undivided Family', F: 'Firm / LLP',
        A: 'Association of Persons', T: 'Trust', B: 'Body of Individuals',
        L: 'Local Authority', J: 'Artificial Juridical Person', G: 'Government'
    };
    const EMAIL_RE = /^[A-Za-z0-9.!#$%&'*+\/=?^_`{|}~-]+@[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)*\.[A-Za-z]{2,}$/;

    function panType() {
        const v = $('PANNumber').value.toUpperCase();
        return /^[A-Z]{5}[0-9]{4}[A-Z]$/.test(v) && PAN_TYPES[v[3]] ? v[3] : null;
    }
    function ageOf(d) {
        const t = new Date();
        let a = t.getFullYear() - d.getFullYear();
        const m = t.getMonth() - d.getMonth();
        if (m < 0 || (m === 0 && t.getDate() < d.getDate())) a--;
        return a;
    }

    /* ---------- rules: validate() returns an error text, or '' when OK ---------- */
    const rules = {
        FullName: {
            validate: function (v) {
                v = v.trim();
                if (!v) return 'Enter your name as per PAN.';
                if (v.length < 3) return 'Name is too short.';
                if (!/^[A-Za-z][A-Za-z .'-]*$/.test(v)) return 'Use letters only (no digits or special characters).';
                return '';
            },
            deps: ['PANNumber']
        },
        PANNumber: {
            validate: function (v) {
                v = v.toUpperCase();
                if (!v) return 'Enter your PAN number.';
                if (!/^[A-Z]{5}[0-9]{4}[A-Z]$/.test(v)) return 'Invalid PAN. Use 5 letters, 4 digits, 1 letter (e.g. ABCPE1234F).';
                if (!PAN_TYPES[v[3]]) return 'The 4th character of a PAN must be A, B, C, F, G, H, J, L, P or T.';
                return '';
            },
            note: function (v) {
                v = v.toUpperCase();
                const type = PAN_TYPES[v[3]];
                const name = $('FullName').value.trim().toUpperCase();
                if (v[3] === 'P' && name && !name.split(/\s+/).some(function (w) { return w[0] === v[4]; })) {
                    return { warn: true, text: 'Valid format (' + type + '). Tip: the 5th letter of an individual PAN is the first letter of the surname - please re-check name and PAN.' };
                }
                return { text: 'Valid PAN format - ' + type };
            },
            deps: ['DateOfBirth']
        },
        DateOfBirth: {
            validate: function (v) {
                if (!v) return 'Select the date of birth as per PAN.';
                const d = new Date(v + 'T00:00:00');
                if (isNaN(d.getTime()) || d.getFullYear() < 1900) return 'Enter a valid date.';
                const today = new Date(); today.setHours(0, 0, 0, 0);
                if (d > today) return 'Date cannot be in the future.';
                if (panType() === 'P' && ageOf(d) < 18) return 'Individual applicants must be at least 18 years old.';
                return '';
            },
            note: function () {
                const t = panType();
                return t && t !== 'P' ? { text: 'For a non-individual PAN this is the date of incorporation.' } : null;
            }
        },
        MobileNumber: {
            validate: function (v) {
                if (!v) return 'Enter your mobile number.';
                if (!/^[0-9]+$/.test(v)) return 'Digits only.';
                if (v.length < 10) return 'Mobile number must be 10 digits.';
                if (!/^[6-9]/.test(v)) return 'Mobile number must start with 6, 7, 8 or 9.';
                if (/^(\d)\1{9}$/.test(v)) return 'Enter a valid mobile number.';
                return '';
            }
        },
        Email: {
            validate: function (v) {
                v = v.trim();
                if (!v) return 'Enter your email address.';
                if (!EMAIL_RE.test(v)) return 'Enter a valid email, e.g. name@example.com';
                return '';
            }
        },
        Address: {
            validate: function (v) {
                v = v.trim();
                if (!v) return 'Enter your address.';
                if (v.length < 10) return 'Enter your full address (at least 10 characters).';
                return '';
            }
        },
        Username: {
            validate: function (v) {
                if (!v) return 'Choose a user name.';
                if (v.length < 4) return 'User name must be at least 4 characters.';
                if (!/^[A-Za-z][A-Za-z0-9._]*$/.test(v)) return 'Start with a letter; use only letters, numbers, dot or underscore.';
                return '';
            }
        },
        Password: {
            validate: function (v) {
                if (!v) return 'Create a password.';
                if (v.length < 6) return 'Password must be 6 to 15 characters.';
                if (/\s/.test(v)) return 'Password cannot contain spaces.';
                return '';
            },
            deps: ['ConfirmPassword']
        },
        ConfirmPassword: {
            validate: function (v) {
                if (!v) return 'Re-enter your password.';
                if (v !== $('Password').value) return 'Passwords do not match.';
                return '';
            },
            note: function () { return { text: 'Passwords match' }; }
        }
    };

    const names = Object.keys(rules);
    names.forEach(function (n) {
        const r = rules[n];
        r.wrap = form.querySelector('.rv-field[data-field="' + n + '"]');
        r.el = $(n);
        r.msg = r.wrap ? r.wrap.querySelector('.rv-msg') : null;
        r.touched = false;
    });

    function clearServerMessage(n) {
        const s = form.querySelector('[data-valmsg-for="' + n + '"]');
        if (s) s.textContent = '';
        if (rules[n].wrap) rules[n].wrap.classList.remove('server-error');
    }

    function check(n, force) {
        const r = rules[n];
        const value = r.el.value;
        const err = r.validate(value);
        const showErr = !!err && (r.touched || force);
        r.wrap.classList.toggle('is-invalid', showErr || r.wrap.classList.contains('server-error'));
        r.wrap.classList.toggle('is-valid', !err && value.trim() !== '' && !r.wrap.classList.contains('server-error'));
        const note = !err && r.note && value.trim() !== '' ? r.note(value) : null;
        r.msg.className = 'rv-msg' + (showErr ? ' err' : note ? (note.warn ? ' warn' : ' ok') : '');
        r.msg.textContent = showErr ? '\u2716 ' + err : note ? (note.warn ? '\u26A0 ' : '\u2714 ') + note.text : '';
        return !err;
    }

    function updateProgress() {
        let done = 0;
        names.forEach(function (n) { if (!rules[n].validate(rules[n].el.value)) done++; });
        $('rvProgressBar').style.width = Math.round(done / names.length * 100) + '%';
        $('rvProgressText').textContent = done + ' of ' + names.length + ' fields completed';
    }

    /* ---------- password strength ---------- */
    function updateStrength() {
        const v = $('Password').value;
        const has = {
            len: v.length >= 6 && v.length <= 15,
            case: /[a-z]/.test(v) && /[A-Z]/.test(v),
            num: /[0-9]/.test(v),
            sym: /[^A-Za-z0-9]/.test(v)
        };
        document.querySelectorAll('#rvRules li').forEach(function (li) {
            li.classList.toggle('met', !!has[li.getAttribute('data-rule')]);
        });
        const box = $('rvStrength');
        const bars = box.querySelectorAll('.rv-strength-bars span');
        const label = $('rvStrengthLabel');
        if (!v) {
            box.setAttribute('data-level', ''); label.textContent = '';
            bars.forEach(function (b) { b.classList.remove('on'); });
            return;
        }
        let score = (v.length >= 8 ? 1 : 0) + (has.case ? 1 : 0) + (has.num ? 1 : 0) + (has.sym ? 1 : 0);
        let level = 'weak', text = 'Weak';
        if (v.length < 6) { score = 1; text = 'Too short'; }
        else if (score >= 3) { level = 'strong'; text = 'Strong'; }
        else if (score === 2) { level = 'okay'; text = 'Okay'; }
        box.setAttribute('data-level', level);
        label.textContent = text;
        bars.forEach(function (b, i) { b.classList.toggle('on', i < Math.max(1, score)); });
    }

    /* ---------- input clean-up + events ---------- */
    function sanitize(n) {
        const el = rules[n].el;
        if (n === 'PANNumber') el.value = el.value.toUpperCase().replace(/[^A-Z0-9]/g, '');
        if (n === 'MobileNumber') el.value = el.value.replace(/\D/g, '');
        if (n === 'FullName') el.value = el.value.replace(/\s{2,}/g, ' ').toUpperCase();
        if (n === 'Username' || n === 'Password' || n === 'ConfirmPassword') el.value = el.value.replace(/\s/g, '');
    }

    names.forEach(function (n) {
        const r = rules[n];
        r.el.addEventListener('input', function () {
            sanitize(n);
            clearServerMessage(n);
            check(n, false);
            (r.deps || []).forEach(function (d) { if (rules[d].touched || rules[d].el.value) check(d, false); });
            if (n === 'Password') updateStrength();
            updateProgress();
        });
        r.el.addEventListener('change', function () {
            r.touched = true; check(n, false);
            (r.deps || []).forEach(function (d) { check(d, false); });
            updateProgress();
        });
        r.el.addEventListener('blur', function () {
            r.touched = true; check(n, false);
        });
    });

    // show / hide password
    form.querySelectorAll('.rv-eye').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const input = $(btn.getAttribute('data-target'));
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.innerHTML = show ? '<i class="fas fa-eye-slash"></i>' : '<i class="fas fa-eye"></i>';
        });
    });

    /* ---------- submit ---------- */
    form.addEventListener('submit', function (e) {
        names.forEach(function (n) { sanitize(n); });
        $('FullName').value = $('FullName').value.trim().toUpperCase();

        let first = null;
        names.forEach(function (n) {
            rules[n].touched = true;
            if (!check(n, true) && !first) first = rules[n];
        });
        updateProgress();

        const summary = $('rvSummary');
        if (first) {
            e.preventDefault();
            summary.classList.remove('d-none');
            first.el.focus();
            first.wrap.scrollIntoView({ block: 'center', behavior: 'smooth' });
            return;
        }
        summary.classList.add('d-none');
        const btn = $('rvSubmit');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Creating account...';
    });

    /* ---------- initial state (also runs when the popup re-opens after a server error) ---------- */
    const dob = $('DateOfBirth');
    const today = new Date();
    dob.min = '1900-01-01';
    dob.max = today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0');
    // an empty date field is posted by the server as 0001-01-01; show it as empty instead
    if (dob.value && new Date(dob.value + 'T00:00:00').getFullYear() < 1900) dob.value = '';

    // messages that came back from the server (e.g. "username already taken")
    names.forEach(function (n) {
        const s = form.querySelector('[data-valmsg-for="' + n + '"]');
        if (s && s.textContent.trim() !== '') rules[n].wrap.classList.add('server-error');
    });

    names.forEach(function (n) { check(n, false); });
    updateStrength();
    updateProgress();

    // re-enable the button if the user comes back to the popup
    modal.addEventListener('shown.bs.modal', function () {
        const btn = $('rvSubmit');
        btn.disabled = false;
        btn.textContent = 'Register';
    });
})();
