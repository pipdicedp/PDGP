$(document).ready(function () {

    var applicationId = $('#hdnApplicationId').val();

    function getApplicationId() {
        return $('#hdnApplicationId').val() || $('#ApplicationId').val();
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    // ---- Reload previously-saved partners from the database (fixes the
    // "Previous button shows empty fields" issue — the grid used to only
    // ever reflect what was added in the CURRENT browser session). ----
    function loadPartners() {
        var appId = getApplicationId();
        if (!appId) return;

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/GetPartnersList',
            type: 'GET',
            data: { applicationId: appId },
            success: function (data) {
                $('#tblPartners tbody').empty();
                if (data && data.length > 0) {
                    data.forEach(function (p) {
                        $('#tblPartners tbody').append(`
                        <tr>
                            <td>${p.partnerName}</td>
                            <td>${p.designation}</td>
                            <td>${p.address}</td>
                            <td>
                                <button type="button" class="btn btn-danger btn-sm btnRemovePartner">
                                    Delete
                                </button>
                            </td>
                        </tr>
                    `);
                    });
                    $('#partnerTableContainer').show();
                }
            }
        });
    }

    // Reload whenever the Partners tab is (re)shown, and once now in case
    // the wizard opens directly on this tab (e.g. via "Continue" from the dashboard).
    $(document).on('wizard:tabShown', function (e, tabName) {
        if (tabName === 'partners') loadPartners();
    });
    loadPartners();

    // ---- Add: local grid only, nothing hits the database yet ----
    // Use event delegation with $(document) to handle dynamically loaded content
    $(document).on('click', '#btnAddPartner', function () {

        var partnerName = $('#PartnerName').val().trim();
        var designation = $('#Designation').val().trim();
        var address = $('#PartnerAddress').val().trim();

        if (!partnerName || !designation || !address) {
            alert('Please enter all partner details');
            return;
        }

        $('#tblPartners tbody').append(`
        <tr>
            <td>${partnerName}</td>
            <td>${designation}</td>
            <td>${address}</td>
            <td>
                <button type="button"
                        class="btn btn-danger btn-sm btnRemovePartner">
                    Delete
                </button>
            </td>
        </tr>
    `);

        // Show table after first record added
        $('#partnerTableContainer').show();

        // Clear fields
        $('#PartnerName').val('');
        $('#Designation').val('');
        $('#PartnerAddress').val('');
        $('#PartnerName').focus();
    });

    // ---- Remove a row from the grid before it's saved ----
    $(document).on('click', '.btnRemovePartner', function () {
        $(this).closest('tr').remove();
        if ($('#tblPartners tbody tr').length === 0) {
            $('#partnerTableContainer').hide();
        }
    });

    // ---- Save: sends every row currently in the grid to the server at once ----
    $(document).on('click', '#btnSavePartners', function () {

        var partners = [];
        $('#tblPartners tbody tr').each(function () {
            var cells = $(this).find('td');
            partners.push({
                partnerName: $(cells[0]).text().trim(),
                designation: $(cells[1]).text().trim(),
                address: $(cells[2]).text().trim()
            });
        });

        if (partners.length === 0) {
            alert('Please add at least one partner before saving.');
            return;
        }

        var applicationId = $('#hdnApplicationId').val();

        var $btn = $(this);
        var originalText = $btn.text();
        $btn.prop('disabled', true).text('Saving...');

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SaveAllPartners',
            type: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: JSON.stringify({
                applicationId: applicationId,
                partners: partners
            }),
            success: function () {
                alert('Partners saved successfully.');
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.error) || 'Failed to save partners. Please try again.';
                alert(msg);
            },
            complete: function () {
                $btn.prop('disabled', false).text(originalText);
            }
        });
    });

});
