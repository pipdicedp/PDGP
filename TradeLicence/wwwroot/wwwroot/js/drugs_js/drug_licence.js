/* Restricted Drug Licence (Form 20A / 21A) - client side.
   - "Pharmacist / Competent Details" box: Add / Edit / Remove any number of rows
   - quick field validation on Submit (the server validates again - edit RULES below to change client checks)
   - PDF / 1 MB check on the file inputs
   Nothing here is required for the data to be safe: DrugLicenceFormService re-checks everything. */
(function () {
    'use strict';

    // ---------------- editable validation rules ----------------
    var RULES = {
        mobile: /^[6-9][0-9]{9}$/,
        pin: /^[1-9][0-9]{5}$/,
        pan: /^[A-Z]{5}[0-9]{4}[A-Z]$/,
        email: /^[^@\s]+@[^@\s]+\.[^@\s]+$/,
        personName: /^[A-Za-z\u00C0-\u024F\u0B80-\u0BFF][A-Za-z\u00C0-\u024F\u0B80-\u0BFF\s.'\-]*$/,
        maxFileBytes: 1048576
    };

    var form = document.getElementById('drugForm');
    if (!form) return;

    // ---------------- pharmacists ----------------
    var rows = [];
    try { rows = JSON.parse(document.getElementById('pharmacistData').textContent) || []; } catch (e) { rows = []; }
    var editIndex = -1;   // not used for state, kept simple: Edit removes the row and loads it into the box

    var box = {
        name: byId('ph_Name'), sex: byId('ph_Sex'), qualification: byId('ph_Qualification'),
        experience: byId('ph_Experience'), councilNo: byId('ph_CouncilNo'), validityDate: byId('ph_ValidityDate'),
        dateOfBirth: byId('ph_DateOfBirth'), yearOfPassing: byId('ph_YearOfPassing')
    };
    var errEl = byId('ph_Error');
    var tbody = document.querySelector('#pharmacistTable tbody');
    var hiddenWrap = byId('pharmacistHidden');
    var emptyMsg = byId('ph_Empty');

    function byId(id) { return document.getElementById(id); }
    function esc(s) { var d = document.createElement('div'); d.textContent = s == null ? '' : String(s); return d.innerHTML; }
    function fmtDate(iso) { if (!iso) return ''; var p = iso.split('-'); return p.length === 3 ? p[2] + '-' + p[1] + '-' + p[0] : iso; }

    function boxIsDirty() {
        return Object.keys(box).some(function (k) { return (box[k].value || '').trim() !== ''; });
    }
    function clearBox() { Object.keys(box).forEach(function (k) { box[k].value = ''; }); errEl.style.display = 'none'; }

    function showBoxError(msg) { errEl.textContent = msg; errEl.style.display = ''; }

    function render() {
        tbody.innerHTML = '';
        hiddenWrap.innerHTML = '';
        rows.forEach(function (r, i) {
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + (i + 1) + '</td><td>' + esc(r.name) + '</td><td>' + esc(r.sex) + '</td><td>' + esc(r.qualification) +
                '</td><td>' + esc(r.experience) + '</td><td>' + esc(r.councilNo) + '</td><td>' + esc(fmtDate(r.validityDate)) +
                '</td><td>' + esc(fmtDate(r.dateOfBirth)) + '</td><td>' + esc(r.yearOfPassing) + '</td>' +
                '<td class="drug-row-actions"><button type="button" class="btn btn-sm btn-outline-secondary" data-edit="' + i + '">Edit</button> ' +
                '<button type="button" class="btn btn-sm btn-outline-danger" data-remove="' + i + '">Remove</button></td>';
            tbody.appendChild(tr);

            [['Name', r.name], ['Sex', r.sex], ['Qualification', r.qualification], ['Experience', r.experience],
             ['CouncilNo', r.councilNo], ['ValidityDate', r.validityDate], ['DateOfBirth', r.dateOfBirth],
             ['YearOfPassing', r.yearOfPassing]].forEach(function (f) {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'Pharmacists[' + i + '].' + f[0];
                input.value = f[1] || '';
                hiddenWrap.appendChild(input);
            });
        });
        byId('pharmacistTable').style.display = rows.length ? '' : 'none';
        emptyMsg.style.display = rows.length ? 'none' : '';
    }

    byId('ph_Add').addEventListener('click', function () {
        var r = {
            name: box.name.value.trim(), sex: box.sex.value, qualification: box.qualification.value.trim(),
            experience: box.experience.value.trim(), councilNo: box.councilNo.value.trim(),
            validityDate: box.validityDate.value, dateOfBirth: box.dateOfBirth.value, yearOfPassing: box.yearOfPassing.value.trim()
        };
        var nowYear = new Date().getFullYear();

        if (!r.name) return showBoxError('Enter the pharmacist / competent person\'s name.');
        if (!RULES.personName.test(r.name)) return showBoxError('Name should contain letters only.');
        if (!r.sex) return showBoxError('Select the sex.');
        if (!r.qualification) return showBoxError('Enter the qualification.');
        if (!r.councilNo) return showBoxError('Enter the council number.');
        if (r.yearOfPassing && (!/^[0-9]{4}$/.test(r.yearOfPassing) || +r.yearOfPassing < 1950 || +r.yearOfPassing > nowYear))
            return showBoxError('Year of passing must be a 4-digit year between 1950 and ' + nowYear + '.');

        rows.push(r);
        clearBox();
        render();
    });

    document.getElementById('pharmacistTable').addEventListener('click', function (e) {
        var rm = e.target.closest('[data-remove]');
        var ed = e.target.closest('[data-edit]');
        if (rm) {
            rows.splice(+rm.getAttribute('data-remove'), 1);
            render();
        } else if (ed) {
            if (boxIsDirty() && !confirm('The box above has details that were not added. Replace them?')) return;
            var i = +ed.getAttribute('data-edit');
            var r = rows[i];
            box.name.value = r.name; box.sex.value = r.sex; box.qualification.value = r.qualification;
            box.experience.value = r.experience; box.councilNo.value = r.councilNo;
            box.validityDate.value = r.validityDate; box.dateOfBirth.value = r.dateOfBirth; box.yearOfPassing.value = r.yearOfPassing;
            rows.splice(i, 1);
            render();
            box.name.focus();
        }
    });

    render();

    // ---------------- cold storage proof star ----------------
    var star = byId('coldProofStar');
    function syncColdStar() {
        var yes = form.querySelector('input[name="ColdStorage"][value="true"]');
        if (star) star.style.display = yes && yes.checked ? '' : 'none';
    }
    form.querySelectorAll('input[name="ColdStorage"]').forEach(function (r) { r.addEventListener('change', syncColdStar); });
    syncColdStar();

    // ---------------- file checks ----------------
    form.querySelectorAll('.drug-file').forEach(function (input) {
        input.addEventListener('change', function () {
            var holder = input.closest('.drug-upload').querySelector('.drug-file-error');
            holder.textContent = '';
            var f = input.files && input.files[0];
            if (!f) return;
            if (!/\.pdf$/i.test(f.name)) { holder.textContent = 'Only PDF files are allowed.'; input.value = ''; return; }
            if (f.size > RULES.maxFileBytes) { holder.textContent = 'File is larger than 1 MB.'; input.value = ''; return; }
        });
    });

    // ---------------- submit-time validation ----------------
    function val(name) { var el = form.querySelector('[name="' + name + '"]'); return el ? (el.value || '').trim() : ''; }
    function checked(name, value) { var el = form.querySelector('input[name="' + name + '"][value="' + value + '"]'); return !!(el && el.checked); }

    function validateForSubmit() {
        var errs = [];
        if (!val('ApplicantName')) errs.push('Applicant name is required.');
        else if (!RULES.personName.test(val('ApplicantName'))) errs.push('Applicant name should contain letters only.');
        if (!val('FirmName')) errs.push('Firm name is required.');
        if (!val('ManagerInchargeName')) errs.push('Manager / Incharge name is required.');
        else if (!RULES.personName.test(val('ManagerInchargeName'))) errs.push('Manager / Incharge name should contain letters only.');
        if (!val('ConstitutionType')) errs.push('Select the constitution type.');
        if (!val('AddressOfTheFirm')) errs.push('Address of the firm is required.');
        if (!val('Districts')) errs.push('Select the district.');
        if (!RULES.pin.test(val('PostalZipCode'))) errs.push('Enter a valid 6-digit postal code.');
        if (!RULES.email.test(val('EMail'))) errs.push('Enter a valid e-mail address.');
        if (!RULES.mobile.test(val('MobileNumber'))) errs.push('Enter a valid 10-digit mobile number.');
        if (!val('TypeOfCeilings')) errs.push('Select the type of ceiling.');
        if (!RULES.pan.test(val('PanCardNo').toUpperCase())) errs.push('Enter a valid PAN (e.g. ABCDE1234F).');
        if (rows.length === 0) errs.push('Add at least one Pharmacist / Competent person.');
        if (!form.querySelector('#storageGroup input[type=checkbox]:checked')) errs.push('Select at least one storage detail.');

        form.querySelectorAll('.drug-upload').forEach(function (block) {
            var key = block.getAttribute('data-doc-key');
            var required = key !== 'Others' && (key !== 'ColdStorageProof' || checked('ColdStorage', 'true'));
            var hasNew = block.querySelector('input[type=file]').files.length > 0;
            var hasOld = !!block.querySelector('.drug-uploaded');
            if (required && !hasNew && !hasOld) errs.push('Upload: ' + block.querySelector('label').textContent.replace('*', '').trim() + '.');
        });
        return errs;
    }

    var pending = null;   // which button was clicked
    form.querySelectorAll('button[type=submit]').forEach(function (b) {
        b.addEventListener('click', function () { pending = b.value; });
    });

    form.addEventListener('submit', function (e) {
        if (boxIsDirty()) {
            e.preventDefault();
            showBoxError('You have pharmacist details in this box that are not added yet. Click "Add" (or clear the box) before saving.');
            byId('pharmacistBox').scrollIntoView({ behavior: 'smooth', block: 'center' });
            return;
        }
        if (pending === 'submit') {
            var errs = validateForSubmit();
            if (errs.length) {
                e.preventDefault();
                var sum = document.querySelector('.drug-summary');
                if (sum) {
                    sum.classList.remove('validation-summary-valid');
                    sum.classList.add('validation-summary-errors');
                    sum.innerHTML = '<ul>' + errs.map(function (m) { return '<li>' + esc(m) + '</li>'; }).join('') + '</ul>';
                    sum.style.display = 'block';
                    sum.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            }
        }
    });
})();
