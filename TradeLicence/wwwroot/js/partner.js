$(document).ready(function () {

    $('#btnAddPartner').on('click', function () {

        var partnerName = $('#PartnerName').val();
        var designation = $('#Designation').val();
        var address = $('#PartnerAddress').val();

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
                        class="btn btn-danger btn-sm btnDeletePartner">
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
    });

    $(document).on("click",
        ".btnRemovePartner",
        function () {

            $(this).closest("tr").remove();
        });

});