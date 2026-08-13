// Lightweight client-side search + "show N entries" for the status table
// (no external DataTables dependency — plain JS filter over the rendered rows).
(function () {
    var table = document.getElementById("statusTable");
    if (!table) return;

    var searchBox = document.getElementById("statusSearchBox");
    var pageSize = document.getElementById("statusPageSize");
    var rows = Array.prototype.slice.call(table.querySelectorAll("tbody tr"));

    function applyFilters() {
        var term = (searchBox.value || "").toLowerCase();
        var limit = parseInt(pageSize.value, 10);
        var shown = 0;

        rows.forEach(function (row) {
            var matches = row.textContent.toLowerCase().indexOf(term) !== -1;
            var withinLimit = shown < limit;

            if (matches && withinLimit) {
                row.style.display = "";
                shown++;
            } else {
                row.style.display = "none";
            }
        });
    }

    searchBox.addEventListener("input", applyFilters);
    pageSize.addEventListener("change", applyFilters);
    applyFilters();
})();
