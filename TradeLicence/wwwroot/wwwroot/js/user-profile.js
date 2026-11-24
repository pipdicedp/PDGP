/*
 * Profile popover for the user-circle icon in the top nav bar.
 *
 * Used by both _UserLayout (citizens -> dbo.Users) and _OfficerLayout
 * (officers -> dbo.Officers). The layout decides which one by putting
 * data-profile-url and data-profile-kind ("user" | "officer") on the button.
 *
 * Click the icon  -> box opens and the details are fetched.
 * Click it again  -> box closes. (Clicking outside or pressing Esc also closes it.)
 */
(function () {
    'use strict';

    var btn = document.getElementById('profileToggleBtn');
    var card = document.getElementById('profileCard');
    if (!btn || !card) return;

    var body = card.querySelector('.profile-card-body');
    var url = btn.getAttribute('data-profile-url');
    var kind = btn.getAttribute('data-profile-kind') || 'user';
    var requestSeq = 0; // ignore responses that arrive after the box was closed/reopened

    // Rows shown in the box, per account type. key = JSON property name.
    var FIELDS = {
        user: [
            { key: 'userId', label: 'User ID' },
            { key: 'email', label: 'Email' },
            { key: 'mobileNumber', label: 'Mobile' },
            { key: 'panNumber', label: 'PAN' },
            { key: 'dateOfBirth', label: 'Date of Birth', type: 'date' },
            { key: 'address', label: 'Address' },
            { key: 'isLocked', label: 'Status', type: 'status' },
            { key: 'failedLoginAttempts', label: 'Failed Logins' },
            { key: 'lastLoginDate', label: 'Last Login', type: 'datetime' },
            { key: 'createdDate', label: 'Created', type: 'datetime' }
        ],
        officer: [
            { key: 'officerId', label: 'Officer ID' },
            { key: 'department', label: 'Department' },
            { key: 'designation', label: 'Designation' },
            { key: 'email', label: 'Email' },
            { key: 'isLocked', label: 'Status', type: 'status' },
            { key: 'failedLoginAttempts', label: 'Failed Logins' },
            { key: 'lastLoginDate', label: 'Last Login', type: 'datetime' },
            { key: 'createdDate', label: 'Created', type: 'datetime' },
            { key: 'createdBy', label: 'Created By' }
        ]
    };

    function isOpen() { return !card.hidden; }

    function open() {
        card.hidden = false;
        btn.setAttribute('aria-expanded', 'true');
        load();
    }

    function close() {
        card.hidden = true;
        btn.setAttribute('aria-expanded', 'false');
        requestSeq++; // invalidate any request still in flight
    }

    btn.addEventListener('click', function (e) {
        e.stopPropagation();
        isOpen() ? close() : open();
    });

    document.addEventListener('click', function (e) {
        if (isOpen() && !card.contains(e.target) && !btn.contains(e.target)) close();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && isOpen()) {
            close();
            btn.focus();
        }
    });

    function load() {
        var seq = ++requestSeq;
        showMessage('Loading\u2026');

        fetch(url, {
            method: 'GET',
            credentials: 'same-origin',
            cache: 'no-store',
            headers: {
                'Accept': 'application/json',
                // Makes cookie auth answer 401/403 instead of redirecting to the
                // login page HTML, so an expired session is detectable here.
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(function (res) {
                if (res.status === 401 || res.status === 403) throw new Error('auth');
                if (!res.ok) throw new Error('http');
                return res.json();
            })
            .then(function (data) {
                if (seq !== requestSeq) return;
                render(data);
            })
            .catch(function (err) {
                if (seq !== requestSeq) return;
                showMessage(err && err.message === 'auth'
                    ? 'Your session has expired. Please log in again.'
                    : 'Unable to load details. Click the icon to try again.', true);
            });
    }

    function showMessage(text, isError) {
        body.textContent = '';
        var p = document.createElement('div');
        p.className = 'profile-msg' + (isError ? ' profile-msg-error' : '');
        p.textContent = text;
        body.appendChild(p);
    }

    function render(d) {
        body.textContent = '';

        // Header: avatar + full name + username
        var head = document.createElement('div');
        head.className = 'profile-head';

        var avatar = document.createElement('i');
        avatar.className = 'fa fa-user-circle profile-avatar';
        head.appendChild(avatar);

        var who = document.createElement('div');
        var name = document.createElement('div');
        name.className = 'profile-name';
        name.textContent = d.fullName || d.username || '';
        who.appendChild(name);

        var uname = document.createElement('div');
        uname.className = 'profile-username';
        uname.textContent = '@' + (d.username || '');
        who.appendChild(uname);

        head.appendChild(who);
        body.appendChild(head);

        // Detail rows
        var list = document.createElement('dl');
        list.className = 'profile-list';

        (FIELDS[kind] || FIELDS.user).forEach(function (f) {
            var dt = document.createElement('dt');
            dt.textContent = f.label;

            var dd = document.createElement('dd');
            if (f.type === 'status') {
                var pill = document.createElement('span');
                pill.className = 'profile-pill ' + (d[f.key] ? 'profile-pill-locked' : 'profile-pill-active');
                pill.textContent = d[f.key] ? 'Locked' : 'Active';
                dd.appendChild(pill);
            } else {
                dd.textContent = format(d[f.key], f.type);
            }

            list.appendChild(dt);
            list.appendChild(dd);
        });

        body.appendChild(list);
    }

    function format(value, type) {
        if (value === null || value === undefined || value === '') return '\u2014';

        if (type === 'date') {
            // "yyyy-MM-dd" from the server; build a local date so no timezone shift occurs.
            var p = String(value).split('-');
            if (p.length !== 3) return String(value);
            var day = new Date(+p[0], +p[1] - 1, +p[2]);
            return day.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
        }

        if (type === 'datetime') {
            // ISO string ending in "Z" (UTC) -> shown in the viewer's local time.
            var dt = new Date(value);
            if (isNaN(dt.getTime())) return String(value);
            return dt.toLocaleString('en-IN', {
                day: '2-digit', month: 'short', year: 'numeric',
                hour: '2-digit', minute: '2-digit', hour12: true
            });
        }

        return String(value);
    }
})();
