/*
 * Collapsible sections in the officers' CAF preview.
 *
 * Every section starts collapsed (the server renders .caf-pv-step-collapsed);
 * clicking — or pressing Enter / Space on — a section heading opens or closes
 * it, and "Expand all" / "Collapse all" do every section at once. The
 * applicant's own preview is not collapsible and is not touched by this file.
 */
(function () {
    'use strict';

    var steps = Array.prototype.slice.call(
        document.querySelectorAll('.caf-pv-step[data-collapsible="true"]'));
    if (steps.length === 0) return;

    function setOpen(step, open) {
        step.classList.toggle('caf-pv-step-collapsed', !open);
        var head = step.querySelector('.caf-pv-step-head');
        if (head) head.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    steps.forEach(function (step) {
        var head = step.querySelector('.caf-pv-step-head');
        if (!head) return;

        head.addEventListener('click', function () {
            setOpen(step, step.classList.contains('caf-pv-step-collapsed'));
        });

        head.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                head.click();
            }
        });
    });

    Array.prototype.forEach.call(document.querySelectorAll('[data-caf-expand-all]'), function (btn) {
        btn.addEventListener('click', function () {
            steps.forEach(function (s) { setOpen(s, true); });
        });
    });

    Array.prototype.forEach.call(document.querySelectorAll('[data-caf-collapse-all]'), function (btn) {
        btn.addEventListener('click', function () {
            steps.forEach(function (s) { setOpen(s, false); });
        });
    });
})();
