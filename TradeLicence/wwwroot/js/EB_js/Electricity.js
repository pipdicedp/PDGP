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

// =====================================================================
// Preview / review page  (Views/Electricity/Preview.cshtml)
// Everything the page needs comes from data-* attributes on #ebPreviewPage,
// so the view itself carries no inline JavaScript.
// =====================================================================
document.addEventListener("DOMContentLoaded", function () {
    const page = document.getElementById("ebPreviewPage");
    if (!page) return; // not the preview page

    const myAppsUrl = page.dataset.myAppsUrl;
    const downloadUrl = page.dataset.downloadUrl;
    const appNo = page.dataset.appNo || "";
    const submitSuccess = page.dataset.submitSuccess === "true";

    function esc(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
        });
    }

    // SweetAlert2 — same CDN the officer pages use. If it can't be loaded the
    // callback gets null and we fall back to the browser's confirm()/alert().
    function withSwal(cb) {
        if (window.Swal) { cb(window.Swal); return; }
        const s = document.createElement("script");
        s.src = "https://cdn.jsdelivr.net/npm/sweetalert2@11";
        s.onload = function () { cb(window.Swal); };
        s.onerror = function () { cb(null); };
        document.head.appendChild(s);
    }

    // 1. Applicant photo — show the placeholder if the image can't be loaded
    document.querySelectorAll(".eb-photo").forEach(function (img) {
        function showPlaceholder() {
            img.classList.add("d-none");
            const placeholder = img.nextElementSibling;
            if (placeholder) placeholder.classList.remove("d-none");
        }
        img.addEventListener("error", showPlaceholder);
        // the error may already have fired before this script ran
        if (img.complete && img.naturalWidth === 0) showPlaceholder();
    });

    // 2. Print / Save as PDF
    document.querySelectorAll(".eb-print-btn").forEach(function (btn) {
        btn.addEventListener("click", function () { window.print(); });
    });

    // 3. Document preview modal (PDF / image viewer popup)
    const modalEl = document.getElementById("docPreviewModal");
    const frame = document.getElementById("docPreviewFrame");
    const modalTitle = document.getElementById("docPreviewTitle");

    document.querySelectorAll(".eb-doc-view").forEach(function (btn) {
        btn.addEventListener("click", function () {
            const url = btn.getAttribute("data-url");
            // Bootstrap's JS not on this layout? Fall back to a new tab.
            if (!modalEl || !(window.bootstrap && window.bootstrap.Modal)) {
                window.open(url, "_blank");
                return;
            }
            modalTitle.textContent = btn.getAttribute("data-title") || "Document Preview";
            frame.src = url;
            window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
        });
    });

    // Stop the viewer (e.g. a PDF) once the modal closes.
    if (modalEl && frame) {
        modalEl.addEventListener("hidden.bs.modal", function () { frame.src = "about:blank"; });
    }

    // 4. Submit Application — confirm first
    const submitBtn = document.getElementById("ebSubmitBtn");
    const submitForm = document.getElementById("ebSubmitForm");
    if (submitBtn && submitForm) {
        submitBtn.addEventListener("click", function () {
            withSwal(function (Swal) {
                if (!Swal) {
                    if (confirm("Submit this application? It will be sent to the officer for review.")) submitForm.submit();
                    return;
                }
                Swal.fire({
                    icon: "question",
                    title: "Submit application?",
                    text: "Please confirm every detail is correct. Once submitted, your application goes to the officer for review.",
                    showCancelButton: true,
                    confirmButtonText: "Yes, Submit",
                    cancelButtonText: "Review again",
                    confirmButtonColor: "#1D6E3E",
                    cancelButtonColor: "#6B7280",
                    reverseButtons: true
                }).then(function (r) { if (r.isConfirmed) submitForm.submit(); });
            });
        });
    }

    // 5. After a successful submit — "submitted successfully" alert
    if (submitSuccess) {
        withSwal(function (Swal) {
            if (!Swal) {
                alert("Application submitted successfully. Application No: " + appNo);
                window.location.href = myAppsUrl;
                return;
            }
            Swal.fire({
                icon: "success",
                title: "Application Submitted Successfully!",
                html: 'Your application number is<br><strong style="font-size:1.25em;color:#1a3a52;">' + esc(appNo) + "</strong>",
                showDenyButton: true,
                confirmButtonText: "Go to My Applications",
                denyButtonText: "Download Acknowledgement",
                confirmButtonColor: "#1a3a52",
                denyButtonColor: "#1D6E3E",
                allowOutsideClick: false
            }).then(function (r) {
                if (r.isConfirmed) window.location.href = myAppsUrl;
                else if (r.isDenied) window.location.href = downloadUrl;
            });
        });
    }
});
