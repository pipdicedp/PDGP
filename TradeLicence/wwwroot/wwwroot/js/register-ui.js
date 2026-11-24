/* Registration form: extra client-side validation + small UI helpers.
   View-side only. register-verify.js (PAN / OTP buttons) is NOT changed. */
(function () {
    'use strict';
    const modal = document.getElementById('registerModal');
    if (!modal) return;
    const $ = id => document.getElementById(id);

    const name = $('FullName'), dob = $('DateOfBirth'), email = $('Email');
    const user = $('Username'), pw = $('Password'), cpw = $('ConfirmPassword');
    const pan = $('PANNumber'), mobile = $('MobileNumber');
    const form = modal.querySelector('form');

    /* ---- DOB: no future dates, minimum age 18, not before 1900 ---- */
    const pad = n => String(n).padStart(2, '0');
    const fmt = d => d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
    const today = new Date();
    const eighteen = new Date(today.getFullYear() - 18, today.getMonth(), today.getDate());
    if (dob) { dob.max = fmt(eighteen); dob.min = '1900-01-01'; }

    /* ---- small helper: show / clear a message under a field ---- */
    function setErr(input, msg) {
        let box = input.closest('.mb-3').querySelector('.js-err');
        if (!box) {
            box = document.createElement('div');
            box.className = 'js-err text-danger';
            input.closest('.mb-3').appendChild(box);
        }
        box.textContent = msg || '';
        input.classList.toggle('is-invalid', !!msg);
        input.classList.toggle('is-valid', !msg && input.value.trim() !== '');
        return !msg;
    }

    /* ---- field rules ---- */
    function checkName() {
        const v = name.value.trim();
        if (!v) return setErr(name, 'Name as per PAN is required.');
        if (!/^[A-Za-z .'-]{3,100}$/.test(v)) return setErr(name, 'Use letters only (min 3 characters).');
        return setErr(name, '');
    }
    function checkDob() {
        if (!dob.value) return setErr(dob, 'Select your date of birth as per PAN.');
        const d = new Date(dob.value);
        if (d > eighteen) return setErr(dob, 'You must be at least 18 years old.');
        if (d.getFullYear() < 1900) return setErr(dob, 'Enter a valid date of birth.');
        return setErr(dob, '');
    }
    function checkPan() {
        if (!pan.value) return setErr(pan, 'PAN number is required.');
        if (!/^[A-Z]{3}[ABCFGHLJPT][A-Z][0-9]{4}[A-Z]$/.test(pan.value))
            return setErr(pan, 'Invalid PAN. Format: 5 letters, 4 digits, 1 letter (ABCDE1234F).');
        return setErr(pan, '');
    }
    function checkMobile() {
        if (!mobile.value) return setErr(mobile, 'Mobile number is required.');
        if (!/^[6-9][0-9]{9}$/.test(mobile.value))
            return setErr(mobile, 'Enter 10 digits starting with 6, 7, 8 or 9.');
        return setErr(mobile, '');
    }
    function checkEmail() {
        const v = email.value.trim();
        if (!v) return setErr(email, 'Email is required.');
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(v)) return setErr(email, 'Enter a valid email address.');
        return setErr(email, '');
    }
    function checkUser() {
        const v = user.value.trim();
        if (!v) return setErr(user, 'User name is required.');
        if (!/^[A-Za-z0-9._]{4,15}$/.test(v)) return setErr(user, '4-15 characters: letters, numbers, dot or underscore.');
        return setErr(user, '');
    }
    function checkPw() {
        if (!pw.value) return setErr(pw, 'Password is required.');
        if (pw.value.length < 6 || pw.value.length > 15) return setErr(pw, 'Password must be 6-15 characters.');
        return setErr(pw, '');
    }
    function checkCpw() {
        const t = $('cpText');
        if (!cpw.value) { t.textContent = ''; return setErr(cpw, 'Please confirm your password.'); }
        if (cpw.value !== pw.value) { t.textContent = ''; return setErr(cpw, 'Passwords do not match.'); }
        setErr(cpw, '');
        t.textContent = '✔ Passwords match';
        t.className = 'd-block text-success';
        return true;
    }

    /* ---- typing filters (stop wrong characters early) ---- */
    name.addEventListener('input', () => { name.value = name.value.replace(/[^A-Za-z .'-]/g, ''); });
    // PAN: CSS uppercase is only visual, so make the real value uppercase too
    pan.addEventListener('input', () => { pan.value = pan.value.toUpperCase().replace(/[^A-Z0-9]/g, ''); });
    // Mobile: digits only
    mobile.addEventListener('input', () => { mobile.value = mobile.value.replace(/\D/g, ''); });
    user.addEventListener('input', () => { user.value = user.value.replace(/[^A-Za-z0-9._]/g, ''); });

    /* ---- live validation (after the user leaves a field, then on every key) ---- */
    [[name, checkName], [dob, checkDob], [pan, checkPan], [mobile, checkMobile],
     [email, checkEmail], [user, checkUser], [pw, checkPw], [cpw, checkCpw]].forEach(([el, fn]) => {
        el.addEventListener('blur', fn);
        el.addEventListener(el === dob ? 'change' : 'input', () => {
            if (el.classList.contains('is-invalid') || el.classList.contains('is-valid')) fn();
        });
    });
    pw.addEventListener('input', () => { if (cpw.value) checkCpw(); });

    /* ---- password strength bar ---- */
    pw.addEventListener('input', () => {
        const v = pw.value;
        let score = 0;
        if (v.length >= 6) score++;
        if (v.length >= 10) score++;
        if (/[A-Z]/.test(v) && /[a-z]/.test(v)) score++;
        if (/\d/.test(v)) score++;
        if (/[^A-Za-z0-9]/.test(v)) score++;
        const levels = [
            ['0%', '#e5e7eb', 'Use 6-15 characters. Mix letters, numbers & symbols.'],
            ['25%', '#dc3545', 'Weak'], ['50%', '#fd7e14', 'Fair'],
            ['75%', '#d4a017', 'Good'], ['100%', '#1b7f3b', 'Strong']
        ];
        const l = v ? levels[Math.min(score, 4) || 1] : levels[0];
        $('pwBar').style.width = l[0];
        $('pwBar').style.background = l[1];
        $('pwText').textContent = l[2];
    });

    /* ---- show / hide password eye buttons ---- */
    modal.querySelectorAll('[data-toggle-pass]').forEach(btn => {
        btn.addEventListener('click', () => {
            const input = $(btn.dataset.togglePass);
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.innerHTML = '<i class="fas ' + (show ? 'fa-eye-slash' : 'fa-eye') + '"></i>';
        });
    });

    /* ---- on Register click: check everything, show first error ---- */
    form.addEventListener('submit', e => {
        const ok = [checkName(), checkDob(), checkPan(), checkMobile(),
                    checkEmail(), checkUser(), checkPw(), checkCpw()].every(Boolean);
        if (!ok) {
            e.preventDefault();
            e.stopImmediatePropagation();
            const bad = form.querySelector('.is-invalid');
            if (bad) bad.focus();
        }
    }, true);

    /* ---- clean the form each time the modal is opened fresh ---- */
    modal.addEventListener('hidden.bs.modal', () => {
        form.querySelectorAll('.js-err').forEach(x => x.textContent = '');
        form.querySelectorAll('.is-invalid,.is-valid').forEach(x => x.classList.remove('is-invalid', 'is-valid'));
    });
})();
