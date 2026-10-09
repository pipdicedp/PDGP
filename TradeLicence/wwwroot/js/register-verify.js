(function () {
    'use strict';
    const modal = document.getElementById('registerModal');
    const cfg = document.getElementById('verifyState');
    if (!modal || !cfg) return;

    const form = modal.querySelector('form');
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    const el = id => document.getElementById(id);

    const fullName = el('FullName'), dob = el('DateOfBirth'), pan = el('PANNumber'), mobile = el('MobileNumber');
    const btnPan = el('btnVerifyPan'), panStatus = el('panStatus');
    const btnSend = el('btnSendOtp'), otpBox = el('otpBox'), otpInput = el('otpInput');
    const btnVerifyOtp = el('btnVerifyOtp'), otpTimer = el('otpTimer'), mobileStatus = el('mobileStatus');
    const btnRegister = modal.querySelector('.btn-register');

    // ---- email OTP elements ----
    const email = el('Email');
    const btnSendEmail = el('btnSendEmailOtp'), emailOtpBox = el('emailOtpBox'), emailOtpInput = el('emailOtpInput');
    const btnVerifyEmailOtp = el('btnVerifyEmailOtp'), emailOtpTimer = el('emailOtpTimer'), emailStatus = el('emailStatus');
    const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

    const PAN_REGEX = /^[A-Z]{3}[ABCFGHLJPT][A-Z][0-9]{4}[A-Z]$/;
    const MOBILE_REGEX = /^[6-9][0-9]{9}$/;

    let panVerified = cfg.dataset.pan === '1';
    let mobileVerified = cfg.dataset.mobile === '1';
    let emailVerified = cfg.dataset.email === '1';
    let emailTimerId = null;
    let timerId = null;

    function setStatus(box, ok, msg) {
        box.textContent = (ok ? '✔ ' : '✖ ') + msg;
        box.className = 'verify-status ' + (ok ? 'ok' : 'err');
    }
    function clearStatus(box) { box.textContent = ''; box.className = 'verify-status'; }
    function refresh() { btnRegister.disabled = !(panVerified && mobileVerified && emailVerified); }

    async function post(url, body) {
        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify(body)
            });
            if (!res.ok) return { success: false, message: 'Server error. Please try again.' };
            return await res.json();
        } catch {
            return { success: false, message: 'Network error. Please try again.' };
        }
    }

    /* ---------------- PAN ---------------- */
    function resetPan() {
        panVerified = false;
        btnPan.textContent = 'Verify';
        clearStatus(panStatus);
        refresh();
    }

    pan.addEventListener('input', () => {
        pan.value = pan.value.toUpperCase().replace(/[^A-Z0-9]/g, '');
        resetPan();
        if (pan.value.length === 10 && !PAN_REGEX.test(pan.value))
            setStatus(panStatus, false, 'Invalid PAN format. Example: ABCDE1234F');
    });
    fullName.addEventListener('input', resetPan);
    dob.addEventListener('change', resetPan);

    btnPan.addEventListener('click', async () => {
        const p = pan.value.trim();
        if (!PAN_REGEX.test(p)) return setStatus(panStatus, false, 'Enter a valid PAN, e.g. ABCDE1234F');
        if (!fullName.value.trim()) return setStatus(panStatus, false, 'Enter your name as per PAN first.');
        if (!dob.value || new Date(dob.value).getFullYear() < 1900)
            return setStatus(panStatus, false, 'Select your date of birth first.');

        btnPan.disabled = true; btnPan.textContent = 'Verifying...';
        const r = await post(cfg.dataset.panUrl, { panNumber: p, fullName: fullName.value.trim(), dateOfBirth: dob.value });
        panVerified = !!r.success;
        setStatus(panStatus, panVerified, r.message);
        btnPan.disabled = false;
        btnPan.textContent = panVerified ? 'Verified ✔' : 'Verify';
        refresh();
    });

    /* ---------------- Mobile OTP ---------------- */
    function resetMobile() {
        mobileVerified = false;
        otpBox.classList.add('d-none');
        otpInput.value = '';
        clearStatus(mobileStatus);
        refresh();
    }

    function startTimer(seconds) {
        clearInterval(timerId);
        let left = seconds;
        btnSend.disabled = true;
        otpTimer.textContent = left;
        timerId = setInterval(() => {
            left--;
            otpTimer.textContent = left;
            if (left <= 0) {
                clearInterval(timerId);
                btnSend.disabled = false;
                btnSend.textContent = 'Resend OTP';
            }
        }, 1000);
    }

    mobile.addEventListener('input', () => {
        mobile.value = mobile.value.replace(/\D/g, '');
        resetMobile();
        if (mobile.value.length === 10 && !MOBILE_REGEX.test(mobile.value))
            setStatus(mobileStatus, false, 'Mobile number must start with 6, 7, 8 or 9.');
    });
    otpInput.addEventListener('input', () => { otpInput.value = otpInput.value.replace(/\D/g, ''); });

    btnSend.addEventListener('click', async () => {
        const m = mobile.value.trim();
        if (!MOBILE_REGEX.test(m)) return setStatus(mobileStatus, false, 'Enter a valid 10-digit mobile number.');

        btnSend.disabled = true; btnSend.textContent = 'Sending...';
        const r = await post(cfg.dataset.sendUrl, { mobileNumber: m });
        setStatus(mobileStatus, r.success, r.message);
        if (r.success) {
            otpBox.classList.remove('d-none');
            otpInput.focus();
            startTimer(30);
        } else {
            btnSend.disabled = false;
            btnSend.textContent = 'Send OTP';
        }
    });

    btnVerifyOtp.addEventListener('click', async () => {
        if (!/^\d{6}$/.test(otpInput.value)) return setStatus(mobileStatus, false, 'Enter the 6-digit OTP.');

        btnVerifyOtp.disabled = true;
        const r = await post(cfg.dataset.verifyUrl, { mobileNumber: mobile.value.trim(), otp: otpInput.value });
        btnVerifyOtp.disabled = false;
        setStatus(mobileStatus, !!r.success, r.message);
        if (r.success) {
            mobileVerified = true;
            clearInterval(timerId);
            otpBox.classList.add('d-none');
            btnSend.disabled = true;
            btnSend.textContent = 'Verified ✔';
            refresh();
        }
    });

    /* ---------------- Email OTP ---------------- */
    function resetEmail() {
        emailVerified = false;
        emailOtpBox.classList.add('d-none');
        emailOtpInput.value = '';
        clearInterval(emailTimerId);
        btnSendEmail.disabled = false;
        btnSendEmail.textContent = 'Send OTP';
        clearStatus(emailStatus);
        refresh();
    }

    function startEmailTimer(seconds) {
        clearInterval(emailTimerId);
        let left = seconds;
        btnSendEmail.disabled = true;
        emailOtpTimer.textContent = left;
        emailTimerId = setInterval(() => {
            left--;
            emailOtpTimer.textContent = left;
            if (left <= 0) {
                clearInterval(emailTimerId);
                btnSendEmail.disabled = false;
                btnSendEmail.textContent = 'Resend OTP';
            }
        }, 1000);
    }

    email.addEventListener('input', resetEmail);
    emailOtpInput.addEventListener('input', () => { emailOtpInput.value = emailOtpInput.value.replace(/\D/g, ''); });

    btnSendEmail.addEventListener('click', async () => {
        const e = email.value.trim();
        if (!EMAIL_REGEX.test(e)) return setStatus(emailStatus, false, 'Enter a valid email address.');

        btnSendEmail.disabled = true; btnSendEmail.textContent = 'Sending...';
        const r = await post(cfg.dataset.sendEmailUrl, { email: e });
        setStatus(emailStatus, !!r.success, r.message);
        if (r.success) {
            emailOtpBox.classList.remove('d-none');
            emailOtpInput.focus();
            startEmailTimer(30);
        } else {
            btnSendEmail.disabled = false;
            btnSendEmail.textContent = 'Send OTP';
        }
    });

    btnVerifyEmailOtp.addEventListener('click', async () => {
        if (!/^\d{6}$/.test(emailOtpInput.value)) return setStatus(emailStatus, false, 'Enter the 6-digit OTP.');

        btnVerifyEmailOtp.disabled = true;
        const r = await post(cfg.dataset.verifyEmailUrl, { email: email.value.trim(), otp: emailOtpInput.value });
        btnVerifyEmailOtp.disabled = false;
        setStatus(emailStatus, !!r.success, r.message);
        if (r.success) {
            emailVerified = true;
            clearInterval(emailTimerId);
            emailOtpBox.classList.add('d-none');
            btnSendEmail.disabled = true;
            btnSendEmail.textContent = 'Verified ✔';
            refresh();
        }
    });

    /* ---------------- Initial state (after a failed submit the modal reopens) ---------------- */
    if (panVerified) { setStatus(panStatus, true, 'PAN verified.'); btnPan.textContent = 'Verified ✔'; }
    if (mobileVerified) { setStatus(mobileStatus, true, 'Mobile number verified.'); btnSend.textContent = 'Verified ✔'; btnSend.disabled = true; }
    if (emailVerified) { setStatus(emailStatus, true, 'Email verified.'); btnSendEmail.textContent = 'Verified ✔'; btnSendEmail.disabled = true; }
    refresh();
})();
