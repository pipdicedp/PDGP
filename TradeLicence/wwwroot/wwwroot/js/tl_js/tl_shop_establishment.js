$(document).ready(function () {

    function getApplicationId() {
        return $('#ApplicationId').val() || $('#hdnApplicationId').val();
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    // ---- Reload previously-saved shop/establishment details ----
    // Maps the camelCase JSON keys ASP.NET Core serializes to the actual
    // (differently-named, e.g. "ApplicantNameShop" not "ApplicantName")
    // HTML field ids on this tab.
    var shopFieldMap = {
        applicantName: 'ApplicantNameShop',
        shopOrEstablishmentName: 'ShopOrEstablishmentName',
        registrationPeriod: 'RegistrationPeriod',
        typeOfEstablishment: 'TypeOfEstablishment',
        mobileNumber: 'MobileNumberShop',
        emailId: 'EmailId',
        shopAddressLine1: 'ShopAddressLine1',
        shopAddressLine2: 'ShopAddressLine2',
        shopDistrictRegion: 'ShopDistrictRegion',
        shopCommune: 'ShopCommune',
        shopPinCode: 'ShopPinCode',
        commAddressLine1: 'CommAddressLine1',
        commAddressLine2: 'CommAddressLine2',
        commDistrictRegion: 'CommDistrictRegion',
        commCommune: 'CommCommune',
        commPinCode: 'CommPinCode',
        maxEmployeesProposed: 'MaxEmployeesProposed',
        maleEmployees: 'MaleEmployees',
        femaleEmployees: 'FemaleEmployees',
        transgenderEmployees: 'TransgenderEmployees',
        totalEmployees: 'TotalEmployees',
        managerFullName: 'ManagerFullName',
        managerAddressLine1: 'ManagerAddressLine1',
        managerAddressLine2: 'ManagerAddressLine2',
        managerCountry: 'ManagerCountry',
        managerState: 'ManagerState',
        managerDistrict: 'ManagerDistrict',
        managerPostalZipCode: 'ManagerPostalZipCode',
        managerMobileNumber: 'ManagerMobileNumber',
        migrantWorkersDirect: 'MigrantWorkersDirect',
        migrantWorkersThroughContractor: 'MigrantWorkersThroughContractor',
        dateOfPaymentOfWages: 'DateOfPaymentOfWages',
        formIXNote: 'FormIXNote',
        amountPaid: 'AmountPaid',
        grasReferenceNumber: 'GrasReferenceNumber',
        dateOfPayment: 'DateOfPayment'
    };

    function loadShopEstablishment() {
        var appId = getApplicationId();
        if (!appId) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/GetShopEstablishment',
            type: 'GET',
            data: { applicationId: appId },
            success: function (data) {
                if (!data || !data.shopRegistrationId) return; // nothing saved yet

                Object.keys(shopFieldMap).forEach(function (jsonKey) {
                    var value = data[jsonKey];
                    if (value === null || value === undefined) return;

                    // Date fields come back as full ISO strings (e.g.
                    // "2026-08-13T00:00:00") — <input type="date"> needs
                    // just the "yyyy-MM-dd" part.
                    if (jsonKey === 'dateOfPayment' && typeof value === 'string') {
                        value = value.split('T')[0];
                    }

                    $('#' + shopFieldMap[jsonKey]).val(value);
                });
            }
        });
    }

    $(document).on('wizard:tabShown', function (e, tabName) {
        if (tabName === 'shops') loadShopEstablishment();
    });
    loadShopEstablishment();

    // ---- Keep Total Employees auto-summed from Male + Female + Transgender ----
    function recalcTotalEmployees() {
        var male = parseInt($('#MaleEmployees').val(), 10) || 0;
        var female = parseInt($('#FemaleEmployees').val(), 10) || 0;
        var trans = parseInt($('#TransgenderEmployees').val(), 10) || 0;
        $('#TotalEmployees').val(male + female + trans);
    }
    $('#MaleEmployees, #FemaleEmployees, #TransgenderEmployees').on('input', recalcTotalEmployees);

    // ---- Save on "Next" ----
    $('#btnShopsNext').on('click', function () {

        var applicationId = getApplicationId();
        if (!applicationId) {
            alert('Please save Application Details first.');
            return;
        }

        var payload = {
            applicationId: parseInt(applicationId, 10),

            applicantName: $('#ApplicantNameShop').val().trim(),
            shopOrEstablishmentName: $('#ShopOrEstablishmentName').val().trim(),
            registrationPeriod: $('#RegistrationPeriod').val(),
            typeOfEstablishment: $('#TypeOfEstablishment').val().trim(),
            mobileNumber: $('#MobileNumberShop').val().trim(),
            emailId: $('#EmailId').val().trim(),

            shopAddressLine1: $('#ShopAddressLine1').val().trim(),
            shopAddressLine2: $('#ShopAddressLine2').val().trim(),
            shopDistrictRegion: $('#ShopDistrictRegion').val().trim(),
            shopCommune: $('#ShopCommune').val().trim(),
            shopPinCode: $('#ShopPinCode').val().trim(),

            commAddressLine1: $('#CommAddressLine1').val().trim(),
            commAddressLine2: $('#CommAddressLine2').val().trim(),
            commDistrictRegion: $('#CommDistrictRegion').val().trim(),
            commCommune: $('#CommCommune').val().trim(),
            commPinCode: $('#CommPinCode').val().trim(),

            maxEmployeesProposed: parseInt($('#MaxEmployeesProposed').val(), 10) || null,
            maleEmployees: parseInt($('#MaleEmployees').val(), 10) || null,
            femaleEmployees: parseInt($('#FemaleEmployees').val(), 10) || null,
            transgenderEmployees: parseInt($('#TransgenderEmployees').val(), 10) || null,
            totalEmployees: parseInt($('#TotalEmployees').val(), 10) || 0,

            managerFullName: $('#ManagerFullName').val().trim(),
            managerMobileNumber: $('#ManagerMobileNumber').val().trim(),
            managerAddressLine1: $('#ManagerAddressLine1').val().trim(),
            managerAddressLine2: $('#ManagerAddressLine2').val().trim(),
            managerCountry: $('#ManagerCountry').val().trim(),
            managerState: $('#ManagerState').val().trim(),
            managerDistrict: $('#ManagerDistrict').val().trim(),
            managerPostalZipCode: $('#ManagerPostalZipCode').val().trim(),

            migrantWorkersDirect: parseInt($('#MigrantWorkersDirect').val(), 10) || null,
            migrantWorkersThroughContractor: parseInt($('#MigrantWorkersThroughContractor').val(), 10) || null,

            dateOfPaymentOfWages: $('#DateOfPaymentOfWages').val() || null,
            formIXNote: $('#FormIXNote').val().trim(),
            amountPaid: parseFloat($('#AmountPaid').val()) || null,
            grasReferenceNumber: $('#GrasReferenceNumber').val().trim(),
            dateOfPayment: $('#DateOfPayment').val() || null
        };

        // Mirrors the required (*) fields marked in the Establishment Details section
        if (!payload.applicantName || !payload.shopOrEstablishmentName ||
            !payload.registrationPeriod || !payload.typeOfEstablishment || !payload.mobileNumber) {
            alert('Please fill all required (*) fields before continuing.');
            return;
        }

        var $btn = $(this);
        var originalText = $btn.text();
        $btn.prop('disabled', true).text('Saving...');

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SaveShopEstablishment',
            type: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: JSON.stringify(payload),
            success: function () {
                alert('Shop / Establishment details saved successfully.');
                if (window.populateConfirmSummary) window.populateConfirmSummary();
                if (window.TradeLicenceApply && window.TradeLicenceApply.goToPreviewTab) {
                    window.TradeLicenceApply.goToPreviewTab();
                }
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.error) || 'Failed to save details. Please try again.';
                alert(msg);
            },
            complete: function () {
                $btn.prop('disabled', false).text(originalText);
            }
        });
    });

    // =========================================================
    // Copy Shop Address -> Communication Address
    // =========================================================
    $('#btnCopyToCommAddress').on('click', function () {
        $('#CommAddressLine1').val($('#ShopAddressLine1').val());
        $('#CommAddressLine2').val($('#ShopAddressLine2').val());
        $('#CommDistrictRegion').val($('#ShopDistrictRegion').val());
        $('#CommCommune').val($('#ShopCommune').val());
        $('#CommPinCode').val($('#ShopPinCode').val());
    });

    // =========================================================
    // Employer other than Manager (repeatable table)
    // Same add-locally / save-all-at-once pattern as Partners/Machinery.
    // =========================================================
    function loadEmployers() {
        var appId = getApplicationId();
        if (!appId) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/GetEmployerList',
            type: 'GET',
            data: { applicationId: appId },
            success: function (data) {
                $('#tblEmployer tbody').empty();
                if (data && data.length > 0) {
                    data.forEach(function (e) {
                        $('#tblEmployer tbody').append(`
                        <tr data-existing="true">
                            <td>${e.employerType || ''}</td>
                            <td>${e.employerName || ''}</td>
                            <td>${e.isMinor ? 'Yes' : 'No'}</td>
                            <td>${e.mobileNumber || ''}</td>
                            <td>${e.email || ''}</td>
                            <td>${e.residentialAddress || ''}</td>
                            <td>${e.district || ''}</td>
                            <td>${e.pinCode || ''}</td>
                            <td>${e.rcToBeIssued ? 'Yes' : 'No'}</td>
                            <td><button type="button" class="btn btn-danger btn-sm btnRemoveEmployer">Delete</button></td>
                        </tr>`);
                    });
                    $('#employerTableContainer').show();
                }
            }
        });
    }
    $(document).on('wizard:tabShown', function (e, tabName) { if (tabName === 'shops') loadEmployers(); });
    loadEmployers();

    $('#btnAddEmployer').on('click', function () {
        var name = $('#EmployerName').val().trim();
        if (!name) { alert('Please enter the employer name'); return; }

        var type = $('#EmployerType').val().trim();
        var isMinor = $('#EmployerIsMinor').val() === 'true';
        var mobile = $('#EmployerMobileNumber').val().trim();
        var email = $('#EmployerEmail').val().trim();
        var address = $('#EmployerResidentialAddress').val().trim();
        var district = $('#EmployerDistrict').val().trim();
        var pin = $('#EmployerPinCode').val().trim();
        var rc = $('#EmployerRcToBeIssued').val() === 'true';

        $('#tblEmployer tbody').append(`
        <tr>
            <td>${type}</td><td>${name}</td><td>${isMinor ? 'Yes' : 'No'}</td>
            <td>${mobile}</td><td>${email}</td><td>${address}</td>
            <td>${district}</td><td>${pin}</td><td>${rc ? 'Yes' : 'No'}</td>
            <td><button type="button" class="btn btn-danger btn-sm btnRemoveEmployer">Delete</button></td>
        </tr>`);
        $('#employerTableContainer').show();

        $('#EmployerType,#EmployerName,#EmployerMobileNumber,#EmployerEmail,#EmployerResidentialAddress,#EmployerDistrict,#EmployerPinCode').val('');
        $('#EmployerIsMinor,#EmployerRcToBeIssued').val('false');
        $('#EmployerName').focus();
    });

    $(document).on('click', '.btnRemoveEmployer', function () {
        $(this).closest('tr').remove();
        if ($('#tblEmployer tbody tr').length === 0) $('#employerTableContainer').hide();
    });

    $('#btnSaveEmployers').on('click', function () {
        var employers = [];
        $('#tblEmployer tbody tr').each(function () {
            if ($(this).attr('data-existing') === 'true') return; // already saved
            var c = $(this).find('td');
            employers.push({
                employerType: $(c[0]).text().trim(),
                employerName: $(c[1]).text().trim(),
                isMinor: $(c[2]).text().trim() === 'Yes',
                mobileNumber: $(c[3]).text().trim(),
                email: $(c[4]).text().trim(),
                residentialAddress: $(c[5]).text().trim(),
                district: $(c[6]).text().trim(),
                pinCode: $(c[7]).text().trim(),
                rcToBeIssued: $(c[8]).text().trim() === 'Yes'
            });
        });
        if (employers.length === 0) { alert('No new employers to save.'); return; }

        var appId = getApplicationId();
        if (!appId) { alert('Please save Application Details first.'); return; }

        var $btn = $(this);
        var original = $btn.text();
        $btn.prop('disabled', true).text('Saving...');

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SaveAllEmployers',
            type: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: JSON.stringify({ applicationId: appId, employers: employers }),
            success: function () { alert('Employer details saved successfully.'); loadEmployers(); },
            error: function (xhr) { alert((xhr.responseJSON && xhr.responseJSON.error) || 'Failed to save employer details.'); },
            complete: function () { $btn.prop('disabled', false).text(original); }
        });
    });

    // =========================================================
    // Form IX, Part-A (repeatable table)
    // =========================================================
    function loadPartA() {
        var appId = getApplicationId();
        if (!appId) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/GetFormIXPartAList',
            type: 'GET',
            data: { applicationId: appId },
            success: function (data) {
                $('#tblPartA tbody').empty();
                if (data && data.length > 0) {
                    data.forEach(function (r) {
                        $('#tblPartA tbody').append(`
                        <tr data-existing="true">
                            <td>${r.employeeName || ''}</td><td>${r.sex || ''}</td>
                            <td>${r.fatherHusbandName || ''}</td><td>${r.designation || ''}</td>
                            <td>${r.employeeNumber || ''}</td><td>${r.dateOfEntryIntoService || ''}</td>
                            <td>${r.personCategory || ''}</td><td>${r.shift || ''}</td>
                            <td>${r.timeOfCommencementOfWork || ''}</td><td>${r.restIntervalHours || ''}</td>
                            <td>${r.timeWorkEnds || ''}</td><td>${r.weeklyHoliday || ''}</td>
                            <td><button type="button" class="btn btn-danger btn-sm btnRemovePartA">Delete</button></td>
                        </tr>`);
                    });
                    $('#partATableContainer').show();
                }
            }
        });
    }
    $(document).on('wizard:tabShown', function (e, tabName) { if (tabName === 'shops') loadPartA(); });
    loadPartA();

    $('#btnAddPartA').on('click', function () {
        var name = $('#PartAEmployeeName').val().trim();
        if (!name) { alert('Please enter the employee name'); return; }

        var vals = ['#PartASex', '#PartAFatherHusbandName', '#PartADesignation', '#PartAEmployeeNumber',
            '#PartADateOfEntryIntoService', '#PartAPersonCategory', '#PartAShift',
            '#PartATimeOfCommencementOfWork', '#PartARestIntervalHours', '#PartATimeWorkEnds', '#PartAWeeklyHoliday']
            .map(function (id) { return $(id).val(); });

        $('#tblPartA tbody').append(`
        <tr>
            <td>${name}</td><td>${vals[0]}</td><td>${vals[1]}</td><td>${vals[2]}</td>
            <td>${vals[3]}</td><td>${vals[4]}</td><td>${vals[5]}</td><td>${vals[6]}</td>
            <td>${vals[7]}</td><td>${vals[8]}</td><td>${vals[9]}</td><td>${vals[10]}</td>
            <td><button type="button" class="btn btn-danger btn-sm btnRemovePartA">Delete</button></td>
        </tr>`);
        $('#partATableContainer').show();

        $('#PartAEmployeeName,#PartAFatherHusbandName,#PartADesignation,#PartAEmployeeNumber,#PartADateOfEntryIntoService,#PartAPersonCategory,#PartAShift,#PartATimeOfCommencementOfWork,#PartARestIntervalHours,#PartATimeWorkEnds,#PartAWeeklyHoliday').val('');
        $('#PartASex').val('');
        $('#PartAEmployeeName').focus();
    });

    $(document).on('click', '.btnRemovePartA', function () {
        $(this).closest('tr').remove();
        if ($('#tblPartA tbody tr').length === 0) $('#partATableContainer').hide();
    });

    $('#btnSavePartA').on('click', function () {
        var rows = [];
        $('#tblPartA tbody tr').each(function () {
            if ($(this).attr('data-existing') === 'true') return;
            var c = $(this).find('td');
            rows.push({
                employeeName: $(c[0]).text().trim(), sex: $(c[1]).text().trim(),
                fatherHusbandName: $(c[2]).text().trim(), designation: $(c[3]).text().trim(),
                employeeNumber: $(c[4]).text().trim(), dateOfEntryIntoService: $(c[5]).text().trim() || null,
                personCategory: $(c[6]).text().trim(), shift: $(c[7]).text().trim(),
                timeOfCommencementOfWork: $(c[8]).text().trim(), restIntervalHours: $(c[9]).text().trim(),
                timeWorkEnds: $(c[10]).text().trim(), weeklyHoliday: $(c[11]).text().trim()
            });
        });
        if (rows.length === 0) { alert('No new rows to save.'); return; }

        var appId = getApplicationId();
        if (!appId) { alert('Please save Application Details first.'); return; }

        var $btn = $(this);
        var original = $btn.text();
        $btn.prop('disabled', true).text('Saving...');

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SaveAllFormIXPartA',
            type: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: JSON.stringify({ applicationId: appId, rows: rows }),
            success: function () { alert('Form IX Part-A saved successfully.'); loadPartA(); },
            error: function (xhr) { alert((xhr.responseJSON && xhr.responseJSON.error) || 'Failed to save.'); },
            complete: function () { $btn.prop('disabled', false).text(original); }
        });
    });

    // =========================================================
    // Form IX, Part-B (repeatable table)
    // =========================================================
    function loadPartB() {
        var appId = getApplicationId();
        if (!appId) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/GetFormIXPartBList',
            type: 'GET',
            data: { applicationId: appId },
            success: function (data) {
                $('#tblPartB tbody').empty();
                if (data && data.length > 0) {
                    data.forEach(function (r) {
                        $('#tblPartB tbody').append(`
                        <tr data-existing="true">
                            <td>${r.classOfWorkers || ''}</td>
                            <td>${r.maxRateOfWage != null ? r.maxRateOfWage : ''}</td>
                            <td>${r.minRateOfWage != null ? r.minRateOfWage : ''}</td>
                            <td><button type="button" class="btn btn-danger btn-sm btnRemovePartB">Delete</button></td>
                        </tr>`);
                    });
                    $('#partBTableContainer').show();
                }
            }
        });
    }
    $(document).on('wizard:tabShown', function (e, tabName) { if (tabName === 'shops') loadPartB(); });
    loadPartB();

    $('#btnAddPartB').on('click', function () {
        var cls = $('#PartBClassOfWorkers').val().trim();
        if (!cls) { alert('Please enter the class of workers'); return; }

        var max = $('#PartBMaxRateOfWage').val().trim();
        var min = $('#PartBMinRateOfWage').val().trim();

        $('#tblPartB tbody').append(`
        <tr>
            <td>${cls}</td><td>${max}</td><td>${min}</td>
            <td><button type="button" class="btn btn-danger btn-sm btnRemovePartB">Delete</button></td>
        </tr>`);
        $('#partBTableContainer').show();

        $('#PartBClassOfWorkers,#PartBMaxRateOfWage,#PartBMinRateOfWage').val('');
        $('#PartBClassOfWorkers').focus();
    });

    $(document).on('click', '.btnRemovePartB', function () {
        $(this).closest('tr').remove();
        if ($('#tblPartB tbody tr').length === 0) $('#partBTableContainer').hide();
    });

    $('#btnSavePartB').on('click', function () {
        var rows = [];
        $('#tblPartB tbody tr').each(function () {
            if ($(this).attr('data-existing') === 'true') return;
            var c = $(this).find('td');
            rows.push({
                classOfWorkers: $(c[0]).text().trim(),
                maxRateOfWage: parseFloat($(c[1]).text().trim()) || null,
                minRateOfWage: parseFloat($(c[2]).text().trim()) || null
            });
        });
        if (rows.length === 0) { alert('No new rows to save.'); return; }

        var appId = getApplicationId();
        if (!appId) { alert('Please save Application Details first.'); return; }

        var $btn = $(this);
        var original = $btn.text();
        $btn.prop('disabled', true).text('Saving...');

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SaveAllFormIXPartB',
            type: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: JSON.stringify({ applicationId: appId, rows: rows }),
            success: function () { alert('Form IX Part-B saved successfully.'); loadPartB(); },
            error: function (xhr) { alert((xhr.responseJSON && xhr.responseJSON.error) || 'Failed to save.'); },
            complete: function () { $btn.prop('disabled', false).text(original); }
        });
    });

    // =========================================================
    // Upload Annexure Files (4 fixed slots)
    // =========================================================
    var annexureFields = [
        { inputId: 'AnnexureFeesFile', shortName: 'Fees', documentName: 'Details of Fees Submitted' },
        { inputId: 'AnnexureOccupancyFile', shortName: 'Occupancy', documentName: 'Proof for Legal Occupancy' },
        { inputId: 'AnnexureEmployerIdFile', shortName: 'EmployerId', documentName: 'ID Proof of the Employer' },
        { inputId: 'AnnexurePremisesPhotoFile', shortName: 'PremisesPhoto', documentName: 'Photo of the Premises With Name Board' }
    ];

    function loadAnnexureDocuments() {
        var appId = getApplicationId();
        if (!appId) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/GetAnnexureDocumentsList',
            type: 'GET',
            data: { applicationId: appId },
            success: function (data) {
                if (!data) return;
                annexureFields.forEach(function (field) {
                    var match = data.find(function (d) { return d.documentName === field.documentName; });
                    if (match) {
                        $('#btnAnnexurePreview' + field.shortName).prop('disabled', false).data('documentId', match.annexureDocumentId);
                        $('#btnAnnexureRemove' + field.shortName).prop('disabled', false).data('documentId', match.annexureDocumentId);
                    }
                });
            }
        });
    }
    $(document).on('wizard:tabShown', function (e, tabName) { if (tabName === 'shops') loadAnnexureDocuments(); });
    loadAnnexureDocuments();

    function uploadAnnexureDocument(field) {
        var input = document.getElementById(field.inputId);
        var file = input && input.files.length ? input.files[0] : null;
        if (!file) { alert('Please choose a file first.'); return; }

        var appId = getApplicationId();
        if (!appId) { alert('Please save Application Details first.'); return; }

        var formData = new FormData();
        formData.append('applicationId', appId);
        formData.append('documentName', field.documentName);
        formData.append('file', file);

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SaveAnnexureDocument',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            success: function (data) {
                $('#btnAnnexurePreview' + field.shortName).prop('disabled', false).data('documentId', data.documentId);
                $('#btnAnnexureRemove' + field.shortName).prop('disabled', false).data('documentId', data.documentId);
                alert(field.documentName + ' uploaded successfully.');
            },
            error: function (xhr) {
                alert((xhr.responseJSON && xhr.responseJSON.error) || 'Failed to upload document. Please try again.');
            }
        });
    }

    annexureFields.forEach(function (field) {
        $('#btnUploadAnnexure' + field.shortName).on('click', function () { uploadAnnexureDocument(field); });
    });

    $(document).on('click', '[id^="btnAnnexurePreview"]', function () {
        var docId = $(this).data('documentId');
        if (!docId) return;
        $('#annexureDocumentViewer').attr('src', '/TradeLicence/NewLicence/Apply/ViewAnnexureDocument?documentId=' + docId);
        var modalEl = document.getElementById('annexurePreviewModal');
        if (modalEl && window.bootstrap) new bootstrap.Modal(modalEl).show();
    });

    $(document).on('click', '[id^="btnAnnexureRemove"]', function () {
        var $btn = $(this);
        var docId = $btn.data('documentId');
        if (!docId) return;
        if (!confirm('Remove this document?')) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/DeleteAnnexureDocument',
            type: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: { documentId: docId },
            success: function () {
                var shortName = $btn.attr('id').replace('btnAnnexureRemove', '');
                $('#Annexure' + shortName + 'File').val('');
                $('#btnAnnexurePreview' + shortName).prop('disabled', true).removeData('documentId');
                $btn.prop('disabled', true).removeData('documentId');
            },
            error: function () { alert('Failed to remove document. Please try again.'); }
        });
    });

});
