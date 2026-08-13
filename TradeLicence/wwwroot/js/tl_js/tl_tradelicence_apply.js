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
        var cls = 'alert alert-warning';
        if (type === 'info') cls = 'alert alert-info';
        if (type === 'danger') cls = 'alert alert-danger';
        var html = `<div class="${cls}" role="alert">${msg}</div>`;

        var topContainer = document.getElementById('tl-wizard-message');
        if (topContainer) {
            topContainer.innerHTML = html;
            clearTimeout(topContainer._hideTimer);
            topContainer._hideTimer = setTimeout(function () { topContainer.innerHTML = ''; }, 4000);
        }

        // Mirrors the message right next to the action buttons at the bottom
        // of the currently visible section — that's where the user's
        // attention already is (they just clicked Next), so they don't have
        // to scroll all the way back up to see why nothing happened.
        var visibleSection = document.querySelector('[id$="-section"]:not([style*="display: none"])');
        var bottomContainer = visibleSection ? visibleSection.querySelector('[id$="-message-bottom"]') : null;
        if (bottomContainer) {
            bottomContainer.innerHTML = html;
            clearTimeout(bottomContainer._hideTimer);
            bottomContainer._hideTimer = setTimeout(function () { bottomContainer.innerHTML = ''; }, 4000);
            bottomContainer.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } else if (topContainer) {
            topContainer.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }

    function clearWizardMessage() {
        var topContainer = document.getElementById('tl-wizard-message');
        if (topContainer) {
            topContainer.innerHTML = '';
            clearTimeout(topContainer._hideTimer);
        }
        document.querySelectorAll('[id$="-message-bottom"]').forEach(function (el) {
            el.innerHTML = '';
            clearTimeout(el._hideTimer);
        });
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
            { id: 'preview', text: 'Preview Application' },
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

    // Order defines what "forward" vs "backward" navigation means.
    var wizardTabOrder = ['application', 'partners', 'machinery', 'photo', 'documents', 'shops', 'preview', 'confirm'];

    var wizardTabLabels = {
        application: 'Application Details',
        partners: 'Partners Details',
        machinery: 'Machinery Details',
        photo: 'Upload Photograph',
        documents: 'Upload Documents',
        shops: 'Registration for Shops and Establishments',
        preview: 'Preview Application',
        confirm: 'Confirm'
    };

    function getCurrentTabId() {
        for (var i = 0; i < wizardTabOrder.length; i++) {
            var el = document.getElementById(wizardTabOrder[i] + '-section');
            if (el && el.style.display !== 'none') {
                return wizardTabOrder[i];
            }
        }
        return 'application';
    }

    // Mirrors each tab's own "Next" button validation (see the various
    // btn*Next handlers further below) so the generic tab-link click handler
    // can check "is the tab the user is currently ON complete?" no matter
    // which tab that is — not just Application Details.
    function isTabComplete(tabId) {
        switch (tabId) {
            case 'application':
                return isApplicationComplete();

            case 'partners':
                var ownershipType = $('#OwnershipType').val();
                if (ownershipType === 'Partnership' && $('#tblPartners tbody tr').length === 0) return false;
                return true;

            case 'machinery':
                return $('#tblMachinery tbody tr').length > 0;

            case 'photo':
                var applicantPhotoInput = document.getElementById('ApplicantPhoto');
                var previewImg = document.getElementById('ApplicantPhotoPreview');
                var hasExistingPreview = !!(previewImg && previewImg.getAttribute('src') && previewImg.style.display !== 'none' && !previewImg.src.endsWith('#'));
                var hasNewFile = !!(applicantPhotoInput && applicantPhotoInput.files && applicantPhotoInput.files.length > 0);
                return hasNewFile || hasExistingPreview;

            case 'documents':
                var requiredDocButtons = ['btnPreviewAadhaar', 'btnPreviewPropertyTax', 'btnPreviewBuildingPlan'];
                return requiredDocButtons.every(function (id) {
                    var $btn = $('#' + id);
                    return $btn.length > 0 && !$btn.prop('disabled') && !!$btn.data('documentId');
                });

            case 'shops':
                // Full field-level validation + save already happens in
                // tl_shop_establishment.js on btnShopsNext — this is just a
                // lightweight "has it been filled at all" signal for jumping
                // away via a tab link instead of the Next button.
                return !!$('#ApplicantNameShop').val();

            default:
                return true;
        }
    }

    function attachWizardHandlers() {
        // Single click handler for every wizard tab link. Moving backward (or
        // clicking the tab you're already on) is always allowed; moving
        // forward requires the CURRENT tab — whichever one that is — to be
        // complete first.
        document.addEventListener('click', function (e) {
            var el = (e.target instanceof Element) ? e.target.closest('a.tl-wizard-tab') : null;
            if (!el) return;
            e.preventDefault();

            const targetTab = el.getAttribute('data-tab');
            const currentTab = getCurrentTabId();
            const currentIdx = wizardTabOrder.indexOf(currentTab);
            const targetIdx = wizardTabOrder.indexOf(targetTab);

            if (targetIdx > currentIdx && !isTabComplete(currentTab)) {
                var label = wizardTabLabels[currentTab] || 'This section';
                showWizardMessage(label + ' should be entered first.', 'warning');
                showWizardSection(currentTab);
                window.scrollTo(0, 0);
                return;
            }

            showWizardSection(targetTab);
            window.scrollTo(0, 0);
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

        function loadDoorNumbers(streetId, selectedDoorNumber) {
            if (!streetId) return;
            console.debug('fetch doors for street', streetId);
            const url = (window.TradeLicenceApi?.getDoorNumbersUrl || '/TradeLicence/GetDoorNumbers') + '?streetId=' + encodeURIComponent(streetId);
            fetch(url, { headers: { 'Accept': 'application/json' } })
                .then(r => {
                    if (!r.ok) throw new Error('Network response was not ok: ' + r.status);
                    return r.json();
                })
                .then(data => {
                    console.debug('doors response', data);
                    fillSelect(doorEl, data, 'doorNumberId', 'doorNumberValue', '---Please Select---');
                    if (selectedDoorNumber) {
                        doorEl.value = selectedDoorNumber;
                    }
                })
                .catch(err => {
                    console.error('Failed to load doors', err);
                    showWizardMessage('Failed to load door numbers. See console for details.', 'danger');
                });
        }

        if (streetEl) {
            streetEl.addEventListener('change', function () {
                fillSelect(doorEl, [], null, null, '---Please Select---');
                loadDoorNumbers(this.value, null);
            });
        }

        // On initial load of an EXISTING draft, Municipality/Ward/Area/Street are
        // already pre-selected server-side (via ViewBag SelectList), but that never
        // fires a 'change' event — and Door Number is populated 100% client-side.
        // Without this, DoorNumber silently stayed empty for any reopened draft,
        // even though a value was already saved in the database.
        if (streetEl && streetEl.value) {
            var savedDoorNumber = doorEl ? doorEl.getAttribute('data-selected') : null;
            loadDoorNumbers(streetEl.value, savedDoorNumber);
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
                            $('.tl-step-badge').text('Step: 2 of 8');
                            renderWizardLinks('partners');
                            advanceStep(2);
                            window.scrollTo(0, 0);
                            alert('Application details saved successfully.');
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
                $('.tl-step-badge').text('Step: 1 of 8');
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
            var stepNumbers = { application: 1, partners: 2, machinery: 3, photo: 4, documents: 5, shops: 6, preview: 7, confirm: 8 };
            $('.tl-step-badge').text('Step: ' + (stepNumbers[startTab] || 1) + ' of 8');
        }
    }

    function showWizardSection(tabName) {

        $('#application-section').hide();
        $('#partners-section').hide();
        $('#machinery-section').hide();
        $('#photo-section').hide();
        $('#documents-section').hide();
        $('#shops-section').hide();
        $('#preview-section').hide();
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

            case 'preview':
                $('#preview-section').show();
                loadPreviewContent();
                break;

            case 'confirm':
                $('#confirm-section').show();
                break;
        }

        renderWizardLinks(tabName);
    }

    // Fetches a fresh read-only summary (Application Details, Partners,
    // Machinery, Photos, Documents, Shop registration) every time the
    // Preview tab is shown, so it always reflects the latest saved data —
    // even if the user went back and changed something on an earlier tab.
    function loadPreviewContent() {
        var applicationId = getApplicationId();
        var container = document.getElementById('previewContent');
        if (!container || !applicationId) return;

        container.innerHTML = '<p class="tl-note">Loading preview...</p>';

        fetch('/TradeLicence/NewLicence/Apply/PreviewApplication?applicationId=' + encodeURIComponent(applicationId))
            .then(function (r) {
                if (!r.ok) throw new Error('Failed to load preview (' + r.status + ')');
                return r.text();
            })
            .then(function (html) {
                container.innerHTML = html;
            })
            .catch(function (err) {
                console.error('Failed to load preview:', err);
                container.innerHTML = '<p class="text-danger">Could not load the preview. Please try again.</p>';
            });
    }

    // NOTE: wizard tab link clicks are handled entirely by attachWizardHandlers()
    // (registered from init()). A second, unconditional handler used to live
    // here calling showWizardSection() directly with no validation — it fired
    // on every click alongside the validated handler and silently overrode it,
    // switching tabs (and duplicating visible sections) even when the current
    // tab wasn't complete. Removed.

    $('#btnPartnerNext').on('click', function () {

        // Partners are only mandatory when the ownership type is "Partnership".
        var ownershipType = $('#OwnershipType').val();
        if (ownershipType === 'Partnership' && $('#tblPartners tbody tr').length === 0) {
            alert('Please add and save at least one partner before continuing.');
            return;
        }

        advanceStep(3);
        showWizardSection('machinery');

        $('.tl-step-badge').text('Step: 3 of 8');

        window.scrollTo(0, 0);
    });

    $('#btnBackToPartner').on('click', function () {

        $('#machinery-section').hide();

        $('#partners-section').show();

        $('.tl-step-badge').text('Step: 2 of 8');

        renderWizardLinks('partners');

        window.scrollTo(0, 0);
    });

    $('#btnMachineryNext').on('click', function () {

        if ($('#tblMachinery tbody tr').length === 0) {
            alert('Please add and save at least one machinery item before continuing.');
            return;
        }

        advanceStep(4);
        showWizardSection('photo');

        $('.tl-step-badge').text('Step: 4 of 8');

        window.scrollTo(0, 0);
    });

    $('#btnBackToMachinery').on('click', function () {

        $('#photo-section').hide();

        $('#machinery-section').show();

        $('.tl-step-badge').text('Step: 3 of 8');

        renderWizardLinks('machinery');

        window.scrollTo(0, 0);
    });

    $('#btnPhotoNext').on('click', function () {

        var applicantPhotoInput = document.getElementById('ApplicantPhoto');
        var previewImg = document.getElementById('ApplicantPhotoPreview');
        var hasExistingPreview = previewImg && previewImg.src && previewImg.style.display !== 'none' &&
            !previewImg.src.endsWith('#') && previewImg.getAttribute('src');
        var hasNewFile = applicantPhotoInput && applicantPhotoInput.files && applicantPhotoInput.files.length > 0;

        if (!hasNewFile && !hasExistingPreview) {
            alert('Please upload the applicant photograph before continuing.');
            return;
        }

        advanceStep(5);
        showWizardSection('documents');

        $('.tl-step-badge').text('Step: 5 of 8');

        window.scrollTo(0, 0);
    });



    $('#btnBackToPhoto').on('click', function () {

        $('#documents-section').hide();

        $('#photo-section').show();

        $('.tl-step-badge').text('Step: 4 of 8');

        renderWizardLinks('photo');

        window.scrollTo(0, 0);
    });

    $('#btnBackToDocuments').on('click', function () {

        $('#shops-section').hide();

        $('#documents-section').show();

        $('.tl-step-badge').text('Step: 5 of 8');

        renderWizardLinks('documents');

        window.scrollTo(0, 0);
    });


    $('#btnDocumentNext').on('click', function () {

        var requiredDocs = [
            { btnId: 'btnPreviewAadhaar', label: 'Aadhaar Copy' },
            { btnId: 'btnPreviewPropertyTax', label: 'Property Tax Receipt' },
            { btnId: 'btnPreviewBuildingPlan', label: 'Building Plan' }
        ];
        var missing = requiredDocs.filter(function (d) {
            var $btn = $('#' + d.btnId);
            return $btn.length === 0 || $btn.prop('disabled') || !$btn.data('documentId');
        });

        if (missing.length > 0) {
            alert('Please upload the following required document(s): ' + missing.map(function (d) { return d.label; }).join(', '));
            return;
        }

        advanceStep(6);
        showWizardSection('shops');

        $('.tl-step-badge').text('Step: 6 of 8');

        window.scrollTo(0, 0);
    });

    $('#btnBackToShops').on('click', function () {

        $('#confirm-section').hide();

        $('#preview-section').show();
        loadPreviewContent();

        $('.tl-step-badge').text('Step: 7 of 8');

        renderWizardLinks('preview');

        window.scrollTo(0, 0);
    });

    $('#btnPreviewPrevious').on('click', function () {
        showWizardSection('shops');
        $('.tl-step-badge').text('Step: 6 of 8');
        window.scrollTo(0, 0);
    });

    $('#btnPreviewNext').on('click', function () {
        advanceStep(8);
        showWizardSection('confirm');
        $('.tl-step-badge').text('Step: 8 of 8');
        window.scrollTo(0, 0);
    });

    // NOTE: btnShopsNext is intentionally NOT handled here. tl_shop_establishment.js
    // owns that button — it validates required fields and saves via AJAX first,
    // then calls window.TradeLicenceApply.goToPreviewTab() below on success.
    // (A second, unconditional handler used to live here and would advance the
    // tab regardless of whether the save succeeded or validation failed — removed.)

    window.TradeLicenceApply = {
        init: init,
        goToPreviewTab: function () {
            advanceStep(7);
            showWizardSection('preview');
            $('.tl-step-badge').text('Step: 7 of 8');
            window.scrollTo(0, 0);
        }
    };
})();