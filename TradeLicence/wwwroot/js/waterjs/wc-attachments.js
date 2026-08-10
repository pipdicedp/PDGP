// Wires up every mandatory document upload on the water connection form:
//  - rejects anything that isn't a PDF the moment it's chosen
//  - shows a small "attachment" chip (filename + Preview + Remove) once a valid PDF is picked
//  - Preview opens the file in the shared in-page modal (via a local blob URL -- nothing is
//    uploaded just to preview it, and nothing opens in a new tab/window)
//  - Remove clears the selection so the user can pick a different file
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

            function showAttachment(file) {
                revoke(input);
                blobUrls.set(input, URL.createObjectURL(file));

                if (nameEl) {
                    nameEl.textContent = file.name;
                }
                if (attachment) {
                    attachment.hidden = false;
                }
                input.style.display = "none";
            }

            function clearSelection() {
                revoke(input);
                input.value = "";
                input.style.display = "";
                if (attachment) {
                    attachment.hidden = true;
                }
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

                showAttachment(file);
            });

            if (previewBtn) {
                previewBtn.addEventListener("click", function () {
                    var url = blobUrls.get(input);
                    if (!url || !previewModal || !previewFrame) {
                        return;
                    }
                    previewFrame.src = url;
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
