// Progressive enhancement: when Department / Section / Contractor change,
// re-filter the downstream dropdown using the FK relationships defined in
// Section_Master, Contractor_Master and Area_Master.
// All dropdowns start pre-loaded with every option (see WaterConnectionController.PopulateDropdowns),
// so the form still works even with JavaScript disabled.
(function () {
    var deptSelect = document.getElementById('DeptCode');
    var sectionSelect = document.getElementById('SectionCode');
    var contractorSelect = document.getElementById('ContractorCode');
    var areaSelect = document.getElementById('AreaCode');

    if (!deptSelect || !sectionSelect || !contractorSelect || !areaSelect) {
        return;
    }

    function fillSelect(select, items, placeholder) {
        var currentValue = select.value;
        select.innerHTML = '';

        var placeholderOption = document.createElement('option');
        placeholderOption.value = '';
        placeholderOption.textContent = placeholder;
        select.appendChild(placeholderOption);

        items.forEach(function (item) {
            var option = document.createElement('option');
            option.value = item.value;
            option.textContent = item.text;
            select.appendChild(option);
        });

        var stillValid = items.some(function (item) { return String(item.value) === currentValue; });
        if (stillValid) {
            select.value = currentValue;
        }
    }

    function fetchJson(url) {
        return fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (res) { return res.ok ? res.json() : []; })
            .catch(function () { return []; });
    }

    deptSelect.addEventListener('change', function () {
        var deptCode = deptSelect.value;
        var url = '/WaterConnection/GetSections' + (deptCode ? ('?deptCode=' + encodeURIComponent(deptCode)) : '');
        fetchJson(url).then(function (sections) {
            fillSelect(sectionSelect, sections, '-- Select Section --');
            sectionSelect.dispatchEvent(new Event('change'));
        });
    });

    sectionSelect.addEventListener('change', function () {
        var sectionCode = sectionSelect.value;
        var url = '/WaterConnection/GetContractors' + (sectionCode ? ('?sectionCode=' + encodeURIComponent(sectionCode)) : '');
        fetchJson(url).then(function (contractors) {
            fillSelect(contractorSelect, contractors, '-- Select Contractor --');
            contractorSelect.dispatchEvent(new Event('change'));
        });
    });

    contractorSelect.addEventListener('change', function () {
        var contractorCode = contractorSelect.value;
        var url = '/WaterConnection/GetAreas' + (contractorCode ? ('?contractorCode=' + encodeURIComponent(contractorCode)) : '');
        fetchJson(url).then(function (areas) {
            fillSelect(areaSelect, areas, '-- Select Area --');
        });
    });
})();
