$(document).ready(function () {

    var applicationId = $('#hdnApplicationId').val();

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    // ---- Add: local grid only, nothing hits the database yet ----
    $('#btnAddMachinery').on('click', function () {

        var machineryName = $('#MachineryName').val().trim();
        var quantity = $('#Quantity').val().trim();
        var horsePower = $('#HorsePower').val().trim();

        if (!machineryName || !quantity || !horsePower) {
            alert('Please enter all machinery details');
            return;
        }

        $('#tblMachinery tbody').append(`
        <tr>
            <td>${machineryName}</td>
            <td>${quantity}</td>
            <td>${horsePower}</td>
            <td>
                <button type="button"
                        class="btn btn-danger btn-sm btnRemoveMachinery">
                    Delete
                </button>
            </td>
        </tr>
    `);

        $('#machineryTableContainer').show();

        $('#MachineryName').val('');
        $('#Quantity').val('');
        $('#HorsePower').val('');
        $('#MachineryName').focus();
    });

    // ---- Remove a row from the grid before it's saved ----
    $(document).on('click', '.btnRemoveMachinery', function () {
        $(this).closest('tr').remove();
        if ($('#tblMachinery tbody tr').length === 0) {
            $('#machineryTableContainer').hide();
        }
    });

    // ---- Save: sends every row currently in the grid to the server at once ----
    $('#btnSaveMachinery').on('click', function () {

        var machinery = [];
        $('#tblMachinery tbody tr').each(function () {
            var cells = $(this).find('td');
            machinery.push({
                machineryName: $(cells[0]).text().trim(),
                quantity: parseInt($(cells[1]).text().trim(), 10),
                horsePower: parseFloat($(cells[2]).text().trim())
            });
        });

        if (machinery.length === 0) {
            alert('Please add at least one machinery item before saving.');
            return;
        }

        var $btn = $(this);
        var originalText = $btn.text();
        $btn.prop('disabled', true).text('Saving...');

        $.ajax({
            url: '/TradeLicence/NewLicence/Apply/SaveAllMachinery',
            type: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: JSON.stringify({
                applicationId: applicationId,
                machinery: machinery
            }),
            success: function () {
                alert('Machinery details saved successfully.');
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.error) || 'Failed to save machinery details. Please try again.';
                alert(msg);
            },
            complete: function () {
                $btn.prop('disabled', false).text(originalText);
            }
        });
    });

});
