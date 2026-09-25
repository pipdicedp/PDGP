document.addEventListener("DOMContentLoaded", function () {
    // 1. Checkbox toggle logic for document file inputs
    const checkboxes = document.querySelectorAll(".doc-toggle");
    checkboxes.forEach(function (chk) {
        chk.addEventListener("change", function () {
            const targetId = this.getAttribute("data-target");
            const fileInput = document.getElementById(targetId);
            if (fileInput) {
                fileInput.disabled = !this.checked;
                if (!this.checked) {
                    fileInput.value = "";
                }
            }
        });
    });

    // 2. Service Type toggle (Individual vs Organization)
    const serviceTypeSelect = document.getElementById("serviceTypeSelect");
    if (serviceTypeSelect) {
        function updateFormFields() {
            const isOrg = serviceTypeSelect.value === "Organization";
            const genderContainer = document.getElementById("genderContainer");
            const lblApplicantName = document.getElementById("lblApplicantName");
            const txtApplicantName = document.getElementById("txtApplicantName");
            const lblRelativeName = document.getElementById("lblRelativeName");
            const txtRelativeName = document.getElementById("txtRelativeName");

            if (isOrg) {
                // Hide Gender
                if (genderContainer) genderContainer.classList.add("d-none");

                // Change labels & placeholders to Organization structure
                if (lblApplicantName) lblApplicantName.innerHTML = 'Name of Organization <span class="req">*</span>';
                if (txtApplicantName) txtApplicantName.placeholder = "Enter organization Name";

                if (lblRelativeName) lblRelativeName.innerHTML = 'Name of Director/Partner/Managing Director <span class="req">*</span>';
                if (txtRelativeName) txtRelativeName.placeholder = "Enter Relation Name";
            } else {
                // Show Gender
                if (genderContainer) genderContainer.classList.remove("d-none");

                // Restore Individual structure
                if (lblApplicantName) lblApplicantName.innerHTML = 'Name of Applicant <span class="req">*</span>';
                if (txtApplicantName) txtApplicantName.placeholder = "Enter Full Name";

                if (lblRelativeName) lblRelativeName.innerHTML = 'Father / Husband Name <span class="req">*</span>';
                if (txtRelativeName) txtRelativeName.placeholder = "Enter Relative Name";
            }
        }

        serviceTypeSelect.addEventListener("change", updateFormFields);
        updateFormFields(); // Run on initial page load
    }
});

$(function () {
    // 1. Document Upload Toggles 📂
    $(".doc-toggle").on("change", function () {
        const targetId = $(this).data("target");
        const $fileInput = $("#" + targetId);
        if ($fileInput.length) {
            const isChecked = $(this).is(":checked");
            $fileInput.prop("disabled", !isChecked);
            if (!isChecked) {
                $fileInput.val("");
            }
        }
    });

    // 2. Service Type Toggle (Individual vs Organization) 👤🏢
    const $serviceTypeSelect = $("#serviceTypeSelect");
    if ($serviceTypeSelect.length) {
        function updateFormFields() {
            const isOrg = $serviceTypeSelect.val() === "Organization";
            const $genderContainer = $("#genderContainer");
            const $lblApplicantName = $("#lblApplicantName");
            const $txtApplicantName = $("#txtApplicantName");
            const $lblRelativeName = $("#lblRelativeName");
            const $txtRelativeName = $("#txtRelativeName");

            if (isOrg) {
                $genderContainer.addClass("d-none");
                $lblApplicantName.html('Name of Organization <span class="req">*</span>');
                $txtApplicantName.attr("placeholder", "Enter organization Name");
                $lblRelativeName.html('Name of Director/Partner/Managing Director <span class="req">*</span>');
                $txtRelativeName.attr("placeholder", "Enter Relation Name");
            } else {
                $genderContainer.removeClass("d-none");
                $lblApplicantName.html('Name of Applicant <span class="req">*</span>');
                $txtApplicantName.attr("placeholder", "Enter Full Name");
                $lblRelativeName.html('Father / Husband Name <span class="req">*</span>');
                $txtRelativeName.attr("placeholder", "Enter Relative Name");
            }
        }
        $serviceTypeSelect.on("change", updateFormFields);
        updateFormFields();
    }

    // 3. Type of Supply Toggle (Permanent vs Temporary) 🔌📅
    const $supplyTypeSelect = $('select[name="TypeOfSupply"]');
    const $tempDateContainer = $("#temporaryDateContainer");

    if ($supplyTypeSelect.length) {
        function toggleTemporaryDates() {
            const isTemporary = $supplyTypeSelect.val() === "Temporary";
            if (isTemporary) {
                $tempDateContainer.removeClass("d-none");
            } else {
                $tempDateContainer.addClass("d-none");
                $tempDateContainer.find("input[type='date']").val("");
            }
        }
        $supplyTypeSelect.on("change", toggleTemporaryDates);
        toggleTemporaryDates();
    }

    // 4. Existing Power Supply Radio Toggle 🔘⚡
    function toggleExistingSupply() {
        const hasSupply = $("#supplyYes").is(":checked");
        const $box = $("#existingSupplyBox");

        if (hasSupply) {
            $box.removeClass("d-none");
        } else {
            $box.addClass("d-none");
            $box.find(".consumer-segment").val("");
        }
    }
    $('input[name="HasExistingSupply"]').on("change", toggleExistingSupply);
    toggleExistingSupply();
});

// Modal close handler 🪟
function closeSuccessModal() {
    const $modal = $("#successModal");
    if ($modal.length) {
        $modal.removeClass("d-block").addClass("d-none");
        const redirectUrl = $modal.data("redirect-url");
        if (redirectUrl) {
            window.location.href = redirectUrl;
        }
    }
}