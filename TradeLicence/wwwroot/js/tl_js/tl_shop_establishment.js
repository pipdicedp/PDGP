$(document).ready(function () {

    function getApplicationId() {
        return $('#ApplicationId').val() || $('#hdnApplicationId').val();
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

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

});
