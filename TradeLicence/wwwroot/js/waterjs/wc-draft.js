// Handles the "Save Application" button on the New Water Connection form.
// Unlike "Submit Application" (a normal type="submit" that goes through full
// jQuery Validate + server-side ModelState validation), this button posts
// whatever has been entered so far to SaveDraft with no completeness checks,
// and shows a popup telling the user what happened.
(function () {
    document.addEventListener("DOMContentLoaded", function () {
        var saveDraftBtn = document.getElementById("aqSaveDraftBtn");
        var form = document.getElementById("aqWaterForm");
        var applicationIdInput = document.getElementById("aqApplicationId");

        if (!saveDraftBtn || !form) {
            return;
        }

        var draftSavedModalEl = document.getElementById("aqDraftSavedModal");
        var draftCompleteModalEl = document.getElementById("aqDraftCompleteModal");

        function showModal(modalEl) {
            if (modalEl && window.bootstrap) {
                new bootstrap.Modal(modalEl).show();
            }
        }

        saveDraftBtn.addEventListener("click", function () {
            saveDraftBtn.disabled = true;
            var originalText = saveDraftBtn.textContent;
            saveDraftBtn.textContent = "Saving...";

            var formData = new FormData(form);
            var saveUrl = form.getAttribute("action") || window.location.href;
            saveUrl = saveUrl.replace(/\/Index\/?$/i, "/SaveDraft");
            if (saveUrl.indexOf("/SaveDraft") === -1) {
                saveUrl = "/WaterConnection/SaveDraft";
            }

            fetch(saveUrl, {
                method: "POST",
                body: formData,
                headers: { "X-Requested-With": "XMLHttpRequest" }
            })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    if (data.status === "draft") {
                        if (data.applicationId && applicationIdInput) {
                            applicationIdInput.value = data.applicationId;
                        }
                        showModal(draftSavedModalEl);
                    } else if (data.status === "complete") {
                        showModal(draftCompleteModalEl);
                    } else {
                        alert(data.message || "Could not save the draft. Please try again.");
                    }
                })
                .catch(function () {
                    alert("Could not save the draft. Please check your connection and try again.");
                })
                .finally(function () {
                    saveDraftBtn.disabled = false;
                    saveDraftBtn.textContent = originalText;
                });
        });
    });
})();
