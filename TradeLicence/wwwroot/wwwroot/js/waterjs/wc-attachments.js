// Wires up every mandatory document upload on the water connection form:
//  - if the draft/application being resumed already has a document saved for a slot
//    (wrapper has data-existing-doc-url, set by Razor from Model.Existing*DocumentId),
//    shows it as already attached instead of an empty upload box
//  - rejects anything that isn't a PDF the moment it's chosen
//  - shows a small "attachment" chip (filename + Preview + Remove) once a valid PDF is picked
//  - Preview opens the document in the shared in-page modal -- a local blob URL for a
//    freshly-picked file (nothing is uploaded just to preview it), or the saved file's
//    own URL for an existing document. Nothing opens in a new tab/window.
//  - Remove clears the selection (and, for an existing document, the hidden
//    Existing*DocumentId field so the server knows a real re-upload is required)
(function () {
    document.addEventListener("DOMContentLoaded", function () {

        var previewModalEl = document.getElementById("aqPreviewModal");
        var previewFrame = document.getElementById("aqPreviewFrame");
        var previewTitle = document.getElementById("aqPreviewTitle");
        var previewModal = (previewModalEl && window.bootstrap) ? new bootstrap.Modal(previewModalEl) : null;

        // Tracks the current blob URL per file input so it can be revoked (freed) once the
        // file is removed/replaced, or a new one is chosen.
        var blobUrls = new WeakMap();

        function isPdfFile(file) {
            var nameLooksLikePdf = /\.pdf$/i.test(file.name || "");
            var typeLooksLikePdf = file.type === "application/pdf";
            return nameLooksLikePdf || typeLooksLikePdf;
        }

        function revoke(input) {
            var existing = blobUrls.get(input);
            if (existing) {
                URL.revokeObjectURL(existing);
                blobUrls.delete(input);
            }
        }

        function setError(wrapper, message) {
            var errorEl = wrapper.querySelector("[data-doc-error]");
            if (errorEl) {
                errorEl.textContent = message;
            }
        }

        document.querySelectorAll("[data-doc-input]").forEach(function (input) {
            var wrapper = input.closest(".aq-doc-upload");
            if (!wrapper) {
                return;
            }

            var attachment = wrapper.querySelector("[data-doc-attachment]");
            var nameEl = wrapper.querySelector("[data-doc-name]");
            var previewBtn = wrapper.querySelector("[data-doc-preview]");
            var removeBtn = wrapper.querySelector("[data-doc-remove]");
            var existingIdInput = wrapper.querySelector("[data-doc-existing-id]");

            // URL of the document already saved for this slot (null once removed or
            // replaced by a fresh pick). currentPreviewUrl tracks whichever URL --
            // existing or freshly-picked -- Preview should currently open.
            var existingUrl = wrapper.dataset.existingDocUrl || null;
            var currentPreviewUrl = existingUrl;

            function showAttachment(label, previewUrl) {
                if (nameEl) {
                    nameEl.textContent = label;
                }
                if (attachment) {
                    attachment.hidden = false;
                }
                input.style.display = "none";
                currentPreviewUrl = previewUrl;
            }

            function clearExisting() {
                existingUrl = null;
                wrapper.dataset.existingDocUrl = "";
                if (existingIdInput) {
                    existingIdInput.value = "";
                }
            }

            function clearSelection() {
                revoke(input);
                input.value = "";
                input.style.display = "";
                if (attachment) {
                    attachment.hidden = true;
                }
                currentPreviewUrl = null;
                clearExisting();
            }

            // A document already saved for this application -- show it as attached
            // right away instead of an empty upload box the user has to fill again.
            if (existingUrl) {
                showAttachment(wrapper.dataset.existingDocLabel || "Document already on file", existingUrl);
            }

            input.addEventListener("change", function () {
                setError(wrapper, "");

                var file = input.files && input.files[0];
                if (!file) {
                    return;
                }

                if (!isPdfFile(file)) {
                    setError(wrapper, "Only PDF files are allowed.");
                    input.value = "";
                    return;
                }

                revoke(input);
                var blobUrl = URL.createObjectURL(file);
                blobUrls.set(input, blobUrl);
                // A freshly-picked file replaces whatever was previously on file.
                clearExisting();
                showAttachment(file.name, blobUrl);
            });

            if (previewBtn) {
                previewBtn.addEventListener("click", function () {
                    if (!currentPreviewUrl || !previewModal || !previewFrame) {
                        return;
                    }
                    previewFrame.src = currentPreviewUrl;
                    if (previewTitle) {
                        previewTitle.textContent = (nameEl && nameEl.textContent) || "Document Preview";
                    }
                    previewModal.show();
                });
            }

            if (removeBtn) {
                removeBtn.addEventListener("click", clearSelection);
            }
        });

        // Stop the preview iframe rendering the PDF once the modal is closed
        if (previewModalEl && previewFrame) {
            previewModalEl.addEventListener("hidden.bs.modal", function () {
                previewFrame.src = "";
            });
        }
    });
})();
