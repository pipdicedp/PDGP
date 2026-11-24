(function() {
    var panels = document.querySelectorAll('.aq-page .aq-panel');
    var stagePill = document.getElementById('aqStagePill');
    var stageNum = document.getElementById('aqStageNum');
    var stageName = document.getElementById('aqStageName');
    var progressFill = document.getElementById('aqProgressFill');
    var flow = document.querySelector('.aq-flow');

    if (!('IntersectionObserver' in window)) {
        panels.forEach(function(p) { p.classList.add('aq-visible'); });
    } else {
        var revealObserver = new IntersectionObserver(function(entries) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('aq-visible');
                    revealObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });
        panels.forEach(function(p) { revealObserver.observe(p); });

        var stageObserver = new IntersectionObserver(function(entries) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting && stageNum && stageName) {
                    var num = entry.target.getAttribute('data-stage') || '1';
                    stageNum.textContent = num.length < 2 ? '0' + num : num;
                    stageName.textContent = entry.target.getAttribute('data-stage-name') || '';
                }
            });
        }, { threshold: 0, rootMargin: '-45% 0px -45% 0px' });
        panels.forEach(function(p) { stageObserver.observe(p); });
    }

    function updateProgress() {
        if (!flow || !progressFill) { return; }
        var rect = flow.getBoundingClientRect();
        var total = rect.height - window.innerHeight;
        var scrolled = -rect.top;
        var pct = total > 0 ? Math.min(100, Math.max(0, (scrolled / total) * 100)) : 0;
        progressFill.style.width = pct + '%';

        if (stagePill) {
            if (pct > 0.5 && pct < 99.5) {
                stagePill.classList.add('aq-stage-visible');
            } else {
                stagePill.classList.remove('aq-stage-visible');
            }
        }
    }

    window.addEventListener('scroll', updateProgress, { passive: true });
    window.addEventListener('resize', updateProgress);
    updateProgress();
})();
    
