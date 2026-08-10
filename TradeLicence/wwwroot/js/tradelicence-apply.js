(function () {
    function fillSelect(select, items, valueKey, textKey, placeholder) {
        if (!select) return;
        select.innerHTML = `<option value="">${placeholder}</option>`;
        items.forEach(item => {
            const opt = document.createElement('option');
            opt.value = item[valueKey];
            opt.textContent = item[textKey];
            select.appendChild(opt);
        });
    }

    function getApplicationId() {
        var idField = document.getElementById('ApplicationId') || document.getElementById('applicationId');
        return idField ? idField.value : null;
    }

    function getAntiForgeryToken() {
        var tokenField = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenField ? tokenField.value : null;
    }

    /// Tells the server the user has reached this step, so "Continue" from the
    /// dashboard reopens the wizard at the right tab instead of always starting
    /// over at Application Details. Fire-and-forget-ish: logs failures but never
    /// blocks the user from moving forward client-side, since this is a progress
    /// marker, not a data save.
    function advanceStep(step) {
        var applicationId = getApplicationId();
        var token = getAntiForgeryToken();
        if (!applicationId || !token) return;

        var body = new URLSearchParams({ applicationId: applicationId, step: step });
        fetch('/TradeLicence/AdvanceStep', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token },
            body: body
        }).catch(function (err) {
            console.warn('Failed to record wizard progress for step ' + step, err);
        });
    }

    // Helper: show inline message below wizard links
    function showWizardMessage(msg, type) {
        var container = document.getElementById('tl-wizard-message');
        if (!container) return;
        var cls = 'alert alert-warning';
        if (type === 'info') cls = 'alert alert-info';
        if (type === 'danger') cls = 'alert alert-danger';
        container.innerHTML = `<div class="${cls}" role="alert">${msg}</div>`;
        // auto-clear after 4s
        clearTimeout(container._hideTimer);
        container._hideTimer = setTimeout(function () { container.innerHTML = ''; }, 4000);
    }

    function clearWizardMessage() {
        var container = document.getElementById('tl-wizard-message');
        if (!container) return;
        container.innerHTML = '';
        clearTimeout(container._hideTimer);
    }

    // Check a small set of required application fields to determine if application details are entered
    function isApplicationComplete() {
        var ids = ['ApplicantName', 'ApplicantResidentialAddress', 'ApplicantFatherHusbandName', 'AgeOfApplicant', 'MobileNumber', 'OwnershipType'];
        for (var i = 0; i < ids.length; i++) {
            var el = document.getElementById(ids[i]);
            if (!el) return false;
            var val = (el.value || '').toString().trim();
            if (!val) return false;
        }
        return true;
    }

    function renderWizardLinks(activeTab) {
        const items = [
            { id: 'application', text: 'Application Details' },
            { id: 'partners', text: 'Partners Details' },
            { id: 'machinery', text: 'Machinery Details' },
            { id: 'photo', text: 'Upload Photograph' },
            { id: 'documents', text: 'Upload Documents' },
            { id: 'shops', text: 'Registration for Shops and Establishments' },
            { id: 'confirm', text: 'Confirm' }
        ];
        const container = document.querySelector('.tl-wizard-links');
        if (!container) return;
        container.innerHTML = items.map((it, idx) => {
            const sep = idx < items.length - 1 ? '<span class="tl-wizard-sep"> / </span>' : '';
            const cls = it.id === activeTab ? 'tl-wizard-tab active' : 'tl-wizard-tab';
            return `<a href="#" class="${cls}" data-tab="${it.id}">${it.text}</a>${sep}`;
        }).join('');
    }

    function focusFirstInvalid() {
        var ids = ['ApplicantName', 'ApplicantResidentialAddress', 'ApplicantFatherHusbandName', 'AgeOfApplicant', 'MobileNumber', 'OwnershipType'];
        for (var i = 0; i < ids.length; i++) {
            var el = document.getElementById(ids[i]);
            if (el && (el.value || '').toString().trim() === '') {
                try { el.focus(); } catch (e) { }
                return;
            }
        }
    }

    function attachWizardHandlers() {
        // Delegate click events for wizard tabs using event delegation and closest
        document.addEventListener('click', function (e) {
            var el = (e.target instanceof Element) ? e.target.closest('a.tl-wizard-tab') : null;
            if (!el) return;
            e.preventDefault();
            const tab = el.getAttribute('data-tab');
            if (tab !== 'application') {
                // If application details are incomplete, show inline message and keep user on application
                if (!isApplicationComplete()) {
                    showWizardMessage('Application details should be entered first.', 'warning');
                    // ensure application tab stays active and visible
                    renderWizardLinks('application');
                    const app = document.getElementById('application-section');
                    if (app) app.style.display = '';
                    const partners = document.getElementById('partners-section');
                    if (partners) partners.style.display = 'none';
                    document.querySelectorAll('.tl-step-badge').forEach(b => b.textContent = 'Step: 1 of 7');
                    // focus first missing required field
                    focusFirstInvalid();
                }
                // else
                // {
                //     showWizardMessage('This tab is not implemented yet.', 'info');
                // }
                return;
            }
            // Show application section when clicked
            if (tab === 'application') {
                const app = document.getElementById('application-section');
                if (app) app.style.display = '';
                // hide other known sections if present
                const partners = document.getElementById('partners-section');
                if (partners) partners.style.display = 'none';
                document.querySelectorAll('.tl-step-badge').forEach(b => b.textContent = 'Step: 1 of 7');
                renderWizardLinks('application');
                window.scrollTo(0, 0);
            }
        });
    }

    function init() {
        const municipalityEl = document.getElementById('MunicipalityId');
        const wardEl = document.getElementById('WardId');
        const areaEl = document.getElementById('AreaId');
        const streetEl = document.getElementById('StreetId');
        const doorEl = document.getElementById('DoorNumber');

        // Initialize wizard links
        var startTab = window.TradeLicenceStartTab || 'application';
        renderWizardLinks(startTab);
        attachWizardHandlers();

        // Clear wizard message when user modifies any application field
        var appFields = document.querySelectorAll('#application-section input, #application-section textarea, #application-section select');
        appFields.forEach(function (f) {
            f.addEventListener('input', function () { clearWizardMessage(); });
            f.addEventListener('change', function () { clearWizardMessage(); });
        });

        // NOTE on key casing: ASP.NET Core's default Json() helper serializes
        // property names as camelCase (WardId -> "wardId"), regardless of how
        // they're named in the C# class. The keys below MUST match that exact
        // camelCase output, or fillSelect() silently writes `undefined` into
        // every option (this was the original "Ward not loading" bug).

        if (municipalityEl) {
            municipalityEl.addEventListener('change', function () {
                fillSelect(wardEl, [], null, null, '---Please Select---');
                fillSelect(areaEl, [], null, null, '---Please Select---');
                fillSelect(streetEl, [], null, null, '---Please Select---');
                fillSelect(doorEl, [], null, null, '---Please Select---');
                if (!this.value) return;
                console.debug('fetch wards for municipality', this.value);
                const url = (window.TradeLicenceApi?.getWardsUrl || '/TradeLicence/GetWards') + '?municipalityId=' + encodeURIComponent(this.value);
                fetch(url, { headers: { 'Accept': 'application/json' } })
                    .then(r => {
                        if (!r.ok) throw new Error('Network response was not ok: ' + r.status);
                        return r.json();
                    })
                    .then(data => {
                        console.debug('wards response', data);
                        fillSelect(wardEl, data, 'wardId', 'wardName', '---Please Select---');
                    })
                    .catch(err => {
                        console.error('Failed to load wards', err);
                        showWizardMessage('Failed to load wards. See console for details.', 'danger');
                    });
            });
        }

        if (wardEl) {
            wardEl.addEventListener('change', function () {
                fillSelect(areaEl, [], null, null, '---Please Select---');
                fillSelect(streetEl, [], null, null, '---Please Select---');
                fillSelect(doorEl, [], null, null, '---Please Select---');
                if (!this.value) return;
                console.debug('fetch areas for ward', this.value);
                const url = (window.TradeLicenceApi?.getAreasUrl || '/TradeLicence/GetAreas') + '?wardId=' + encodeURIComponent(this.value);
                fetch(url, { headers: { 'Accept': 'application/json' } })
                    .then(r => {
                        if (!r.ok) throw new Error('Network response was not ok: ' + r.status);
                        return r.json();
                    })
                    .then(data => {
                        console.debug('areas response', data);
                        fillSelect(areaEl, data, 'areaId', 'areaName', '---Please Select---');
                    })
                    .catch(err => {
                        console.error('Failed to load areas', err);
                        showWizardMessage('Failed to load areas. See console for details.', 'danger');
                    });
            });
        }

        if (areaEl) {
            areaEl.addEventListener('change', function () {
                fillSelect(streetEl, [], null, null, '---Please Select---');
                fillSelect(doorEl, [], null, null, '---Please Select---');
                if (!this.value) return;
                console.debug('fetch streets for area', this.value);
                const url = (window.TradeLicenceApi?.getStreetsUrl || '/TradeLicence/GetStreets') + '?areaId=' + encodeURIComponent(this.value);
                fetch(url, { headers: { 'Accept': 'application/json' } })
                    .then(r => {
                        if (!r.ok) throw new Error('Network response was not ok: ' + r.status);
                        return r.json();
                    })
                    .then(data => {
                        console.debug('streets response', data);
                        fillSelect(streetEl, data, 'streetId', 'streetName', '---Please Select---');
                    })
                    .catch(err => {
                        console.error('Failed to load streets', err);
                        showWizardMessage('Failed to load streets. See console for details.', 'danger');
                    });
            });
        }

        if (streetEl) {
            streetEl.addEventListener('change', function () {
                fillSelect(doorEl, [], null, null, '---Please Select---');
                if (!this.value) return;
                console.debug('fetch doors for street', this.value);
                const url = (window.TradeLicenceApi?.getDoorNumbersUrl || '/TradeLicence/GetDoorNumbers') + '?streetId=' + encodeURIComponent(this.value);
                fetch(url, { headers: { 'Accept': 'application/json' } })
                    .then(r => {
                        if (!r.ok) throw new Error('Network response was not ok: ' + r.status);
                        return r.json();
                    })
                    .then(data => {
                        console.debug('doors response', data);
                        fillSelect(doorEl, data, 'doorNumberId', 'doorNumberValue', '---Please Select---');
                    })
                    .catch(err => {
                        console.error('Failed to load doors', err);
                        showWizardMessage('Failed to load door numbers. See console for details.', 'danger');
                    });
            });
        }

        // NOTE: the original file had a second, duplicate 'change' handler
        // registered on streetEl here (hardcoded URL + lowercase keys). It has
        // been removed — a second addEventListener('change', ...) on the same
        // element doesn't replace the first, it just runs an extra time and
        // was fighting the handler above. Only one handler per element now.

        const btnCopy = document.getElementById('btnCopyAddress');
        if (btnCopy) {
            btnCopy.addEventListener('click', function () {
                const src = document.getElementById('TradePlaceCommunicationAddress');
                const dest = document.getElementById('BuildingOwnerAddress');
                if (src && dest) dest.value = src.value;
            });
        }
        if (document.getElementById('ApplicantPhoto')) {

            $('#ApplicantPhoto').change(function () {

                previewImage(this, 'ApplicantPhotoPreview');
            });
        }

        if (document.getElementById('PartnerPhoto')) {

            $('#PartnerPhoto').change(function () {

                previewImage(this, 'PartnerPhotoPreview');
            });
        }

        function previewImage(input, previewId) {

            const preview = document.getElementById(previewId);

            if (input.files && input.files[0]) {

                const reader = new FileReader();

                reader.onload = function (e) {

                    preview.src = e.target.result;
                    preview.style.display = "block";
                };

                reader.readAsDataURL(input.files[0]);
            }
        }

        let aadhaarFileUrl = null;

        $('#AadhaarFile').on('change', function () {

            const file = this.files[0];

            if (file) {

                aadhaarFileUrl = URL.createObjectURL(file);

                $('#btnPreviewAadhaar').prop('disabled', false);

                $('#btnRemoveAadhaar').prop('disabled', false);
            }
        });

        $(document).on('click', '#btnPreviewAadhaar', function () {

            const file = $('#AadhaarFile')[0].files[0];

            if (!file) {
                alert('Please select a file');
                return;
            }

            const fileUrl = URL.createObjectURL(file);

            $('#documentViewer').attr('src', fileUrl);

            $('#documentPreviewModal').modal('show');
        });

        $('#btnRemoveAadhaar').on('click', function () {

            $('#AadhaarFile').val('');

            $('#btnPreviewAadhaar').prop('disabled', true);

            $('#btnRemoveAadhaar').prop('disabled', true);

            $('#pdfViewer').attr('src', '');

            aadhaarFileUrl = null;
        });

        // Property Tax

        let propertyTaxFileUrl = null;

        $('#PropertyTaxFile').on('change', function () {

            const file = this.files[0];

            if (file) {

                propertyTaxFileUrl = URL.createObjectURL(file);

                $('#btnPreviewPropertyTax').prop('disabled', false);

                $('#btnRemovePropertyTax').prop('disabled', false);
            }
        });

        $(document).on('click', '#btnPreviewPropertyTax', function () {

            const file = $('#PropertyTaxFile')[0].files[0];

            if (!file) {
                alert('Please select a file');
                return;
            }

            const fileUrl = URL.createObjectURL(file);

            $('#documentViewer').attr('src', fileUrl);

            $('#documentPreviewModal').modal('show');
        });

        $('#btnRemovePropertyTax').on('click', function () {

            $('#PropertyTaxFile').val('');

            $('#btnPreviewPropertyTax').prop('disabled', true);

            $('#btnRemovePropertyTax').prop('disabled', true);

            $('#documentViewer').attr('src', '');

            propertyTaxFileUrl = null;
        });


        // Building Plan

        let buildingPlanFileUrl = null;

        $('#BuildingPlanFile').on('change', function () {

            const file = this.files[0];

            if (file) {

                buildingPlanFileUrl = URL.createObjectURL(file);

                $('#btnPreviewBuildingPlan').prop('disabled', false);

                $('#btnRemoveBuildingPlan').prop('disabled', false);
            }
        });

        $(document).on('click', '#btnPreviewBuildingPlan', function () {

            const file = $('#BuildingPlanFile')[0].files[0];

            if (!file) {
                alert('Please select a file');
                return;
            }

            const fileUrl = URL.createObjectURL(file);

            $('#documentViewer').attr('src', fileUrl);

            $('#documentPreviewModal').modal('show');
        });

        $('#btnRemoveBuildingPlan').on('click', function () {

            $('#BuildingPlanFile').val('');

            $('#btnPreviewBuildingPlan').prop('disabled', true);

            $('#btnRemoveBuildingPlan').prop('disabled', true);

            $('#documentViewer').attr('src', '');

            buildingPlanFileUrl = null;
        });

        $('#MaleEmployees,#FemaleEmployees,#TransgenderEmployees').on('input', function () {

            let male = parseInt($('#MaleEmployees').val()) || 0;
            let female = parseInt($('#FemaleEmployees').val()) || 0;
            let trans = parseInt($('#TransgenderEmployees').val()) || 0;

            $('#TotalEmployees').val(male + female + trans);
        });

        // Wizard: Next -> validate application-section then show partners-section
        try {
            var form = $('#tradeLicenceForm');
            var validator = form.validate();

            $('#btnNext').on('click', function (e) {
                e.preventDefault();
                var valid = true;
                $('#application-section').find(':input').each(function () {
                    if ($(this).is(':disabled') || $(this).attr('type') === 'button') return;
                    if (!validator.element(this)) valid = false;
                });

                if (!valid) {
                    var $err = $('.input-validation-error').first();
                    if ($err.length) {
                        var top = $err.offset().top - 80;
                        window.scrollTo({ top: top, behavior: 'smooth' });
                    }
                    return;
                }

                // Save application details before proceeding
                var formElement = document.getElementById('tradeLicenceForm');
                var formData = new FormData(formElement);
                var saveUrl = (window.TradeLicenceApi?.saveApplicationDetailsUrl || '/TradeLicence/SaveApplicationDetails');
                var token = getAntiForgeryToken();

                fetch(saveUrl, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token },
                    body: formData
                })
                    .then(function (response) {
                        if (!response.ok) {
                            return response.json().then(function (data) {
                                throw new Error('Save failed: ' + (data.errors ? JSON.stringify(data.errors) : response.statusText));
                            });
                        }
                        return response.json();
                    })
                    .then(function (data) {
                        if (data.success) {
                            if (data.applicationId) {
                                document.getElementById('ApplicationId').value = data.applicationId;

                                // Keep the Partners tab's own hidden field in sync too —
                                // it was rendered at page-load time (often with 0 for a
                                // brand-new application) and never updated after this
                                // save, which was sending applicationId=0 to SaveAllPartners.
                                var hdnAppId = document.getElementById('hdnApplicationId');
                                if (hdnAppId) hdnAppId.value = data.applicationId;
                            }
                            $('#application-section').hide();
                            $('#partners-section').show();
                            $('.tl-step-badge').text('Step: 2 of 7');
                            renderWizardLinks('partners');
                            advanceStep(2);
                            window.scrollTo(0, 0);
                        } else {
                            showWizardMessage('Failed to save application details.', 'danger');
                        }
                    })
                    .catch(function (err) {
                        console.error('Error saving application:', err);
                        showWizardMessage('Error saving application. See console for details.', 'danger');
                    });
            });

            $('#btnBackToApplication').on('click', function () {
                $('#partners-section').hide();
                $('#application-section').show();
                $('.tl-step-badge').text('Step: 1 of 7');
                renderWizardLinks('application');
                window.scrollTo(0, 0);
            });
        } catch (e) {
            // If jQuery isn't available or something fails, fail gracefully.
            console.warn('TradeLicenceApply init: jQuery not available or an error occurred', e);
        }

        // Save Draft: still validate the form client-side before submitting,
        // even though it posts to a different formaction (SaveDraft).
        const saveBtn = document.getElementById('btnSaveDraft');
        if (saveBtn) {
            saveBtn.addEventListener('click', function (e) {
                const form = document.getElementById('tradeLicenceForm');
                let valid = true;
                if (window.jQuery && typeof $(form).valid === 'function') {
                    valid = $(form).valid();
                } else if (form && typeof form.reportValidity === 'function') {
                    valid = form.reportValidity();
                }
                if (!valid) {
                    e.preventDefault();
                    showWizardMessage('Please correct validation errors before saving draft.', 'danger');
                }
                // otherwise let the form submit normally to its formaction
            });
        }

        // Resume at the correct step when reopening an existing draft
        // (e.g. via "Continue" from the dashboard). window.TradeLicenceStartTab
        // is rendered server-side from the application's CurrentStep.
        if (startTab && startTab !== 'application' && typeof showWizardSection === 'function') {
            showWizardSection(startTab);
            var stepNumbers = { application: 1, partners: 2, machinery: 3, photo: 4, documents: 5, shops: 6, confirm: 7 };
            $('.tl-step-badge').text('Step: ' + (stepNumbers[startTab] || 1) + ' of 7');
        }
    }

    function showWizardSection(tabName) {

        $('#application-section').hide();
        $('#partners-section').hide();
        $('#machinery-section').hide();
        $('#photo-section').hide();
        $('#documents-section').hide();
        $('#shops-section').hide();
        $('#confirm-section').hide();

        switch (tabName) {

            case 'application':
                $('#application-section').show();
                break;

            case 'partners':
                $('#partners-section').show();
                break;

            case 'machinery':
                $('#machinery-section').show();
                break;

            case 'photo':
                $('#photo-section').show();
                break;

            case 'documents':
                $('#documents-section').show();
                break;

            case 'shops':
                $('#shops-section').show();
                break;

            case 'confirm':
                $('#confirm-section').show();
                break;
        }

        renderWizardLinks(tabName);
    }

    $(document).on('click', '.tl-wizard-tab', function (e) {

        e.preventDefault();

        var tab = $(this).data('tab');

        showWizardSection(tab);
    });

    $('#btnPartnerNext').on('click', function () {

        advanceStep(3);
        showWizardSection('machinery');

        $('.tl-step-badge').text('Step: 3 of 7');

        window.scrollTo(0, 0);
    });

    $('#btnBackToPartner').on('click', function () {

        $('#machinery-section').hide();

        $('#partners-section').show();

        $('.tl-step-badge').text('Step: 2 of 7');

        renderWizardLinks('partners');

        window.scrollTo(0, 0);
    });

    $('#btnMachineryNext').on('click', function () {

        advanceStep(4);
        showWizardSection('photo');

        $('.tl-step-badge').text('Step: 4 of 7');

        window.scrollTo(0, 0);
    });

    $('#btnBackToMachinery').on('click', function () {

        $('#photo-section').hide();

        $('#machinery-section').show();

        $('.tl-step-badge').text('Step: 3 of 7');

        renderWizardLinks('machinery');

        window.scrollTo(0, 0);
    });

    $('#btnPhotoNext').on('click', function () {

        advanceStep(5);
        showWizardSection('documents');

        $('.tl-step-badge').text('Step: 5 of 7');

        window.scrollTo(0, 0);
    });



    $('#btnBackToPhoto').on('click', function () {

        $('#documents-section').hide();

        $('#photo-section').show();

        $('.tl-step-badge').text('Step: 4 of 7');

        renderWizardLinks('photo');

        window.scrollTo(0, 0);
    });

    $('#btnBackToDocuments').on('click', function () {

        $('#shops-section').hide();

        $('#documents-section').show();

        $('.tl-step-badge').text('Step: 5 of 7');

        renderWizardLinks('documents');

        window.scrollTo(0, 0);
    });


    $('#btnDocumentNext').on('click', function () {

        advanceStep(6);
        showWizardSection('shops');

        $('.tl-step-badge').text('Step: 6 of 7');

        window.scrollTo(0, 0);
    });

    $('#btnBackToShops').on('click', function () {

        $('#confirm-section').hide();

        $('#shops-section').show();

        $('.tl-step-badge').text('Step: 6 of 7');

        renderWizardLinks('shops');

        window.scrollTo(0, 0);
    });
    $('#btnShopsNext').on('click', function () {

        advanceStep(7);
        showWizardSection('confirm');

        $('.tl-step-badge').text('Step: 7 of 7');

        window.scrollTo(0, 0);
    });

    window.TradeLicenceApply = {
        init: init
    };
})();
