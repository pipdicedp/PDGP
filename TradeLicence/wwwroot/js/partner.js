$(document).ready(function () {

    // Reads the ApplicationId from the hidden field rendered by
    // _PartnerDetails.cshtml (<input type="hidden" id="hdnApplicationId" ... />)
    var applicationId = $('#hdnApplicationId').val();

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    $('#btnAddPartner').on('click', function () {

        var partnerName = $('#PartnerName').val();
        var designation = $('#Designation').val();
        var address = $('#PartnerAddress').val();

        if (!partnerName || !designation || !address) {
            alert('Please enter all partner details');
            return;
        }

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/AddPartner',
            type: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: {
                applicationId: applicationId,
                partnerName: partnerName,
                designation: designation,
                address: address
            },
            success: function (data) {
                // data.partnerId / partnerName / designation / address come back
                // from the server — this is what makes the row's Delete button
                // able to target the correct database row afterwards.
                $('#tblPartners tbody').append(`
                <tr data-partner-id="${data.partnerId}">
                    <td>${data.partnerName}</td>
                    <td>${data.designation}</td>
                    <td>${data.address}</td>
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
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.error) || 'Failed to add partner. Please try again.';
                alert(msg);
            }
        });
    });

    $(document).on("click", ".btnRemovePartner", function () {

        var $row = $(this).closest("tr");
        var partnerId = $row.data('partner-id');

        if (!partnerId) {
            // Row has no partner-id — it was never actually saved (shouldn't
            // happen with the AJAX version above, but fail safely).
            $row.remove();
            return;
        }

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/DeletePartner',
            type: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: { partnerId: partnerId },
            success: function () {
                $row.remove();
                if ($('#tblPartners tbody tr').length === 0) {
                    $('#partnerTableContainer').hide();
                }
            },
            error: function () {
                alert('Failed to delete partner. Please try again.');
            }
        });
    });

});