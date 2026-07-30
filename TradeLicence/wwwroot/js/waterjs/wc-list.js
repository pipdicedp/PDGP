// Water Connection Applications - List view behavior
(function () {
    document.addEventListener('click', function (e) {
        var link = e.target.closest('.js-confirm-delete');
        if (!link) {
            return;
        }

        if (!confirm('Delete this application?')) {
            e.preventDefault();
        }
    });
})();
