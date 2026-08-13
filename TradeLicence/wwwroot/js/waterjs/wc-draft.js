// Handles the "Save Application" button on the New Water Connection form.
// Unlike "Submit Application" (a normal type="submit" that goes through full
// jQuery Validate + server-side ModelState validation), this button posts
// whatever has been entered so far to SaveDraft with no completeness checks.
// On a successful save it sends the user to the Water Connection home page
// (same as a real submit), where the "draft saved" popup is shown; if the
// form turns out to already be fully filled in, it stays on the form and
// tells the user to use "Submit Application" instead.
(function () {
    document.addEventListener("DOMContentLoaded", function () {
        var saveDraftBtn = document.getElementById("aqSaveDraftBtn");
        var form = document.getElementById("aqWaterForm");

        if (!saveDraftBtn || !form) {
            return;
        }

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
                        // The server already stashed a "draft saved" message in
                        // TempData (cookie-backed), so this plain navigation is enough
                        // for the Home page to pick it up and show the popup there.
                        window.location.href = "/WaterConnection/Home";
                    } else if (data.status === "complete") {
                        showModal(draftCompleteModalEl);
                        saveDraftBtn.disabled = false;
                        saveDraftBtn.textContent = originalText;
                    } else {
                        alert(data.message || "Could not save the draft. Please try again.");
                        saveDraftBtn.disabled = false;
                        saveDraftBtn.textContent = originalText;
                    }
                })
                .catch(function () {
                    alert("Could not save the draft. Please check your connection and try again.");
                    saveDraftBtn.disabled = false;
                    saveDraftBtn.textContent = originalText;
                });
        });
    });
})();
