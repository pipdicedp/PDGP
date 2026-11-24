// site.js
// Lightweight, dependency-free replacements for the interactive behaviour
// the original Next.js page got from Lenis (smooth scroll), Framer Motion
// (mount / scroll animations) and react-fast-marquee (the top ticker).
// See README.md for the full list of what this does and does not cover.

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        revealMountAnimations();
        setupNavbar();
        setupDropdowns();
        duplicateMarquees();
        animateVisitorCounter();
        setupClickToPlayVideos();
        setupHeroVideoFallback();
    });

    // ------------------------------------------------------------------
    // 1) Fade in elements that were captured mid Framer-Motion transition
    //    (class="... opacity-0") without also being a deliberately-hidden
    //    interactive panel (those additionally carry "invisible" and
    //    "pointer-events-none").
    // ------------------------------------------------------------------
    function revealMountAnimations() {
        var candidates = document.querySelectorAll(".opacity-0");
        candidates.forEach(function (el) {
            if (el.classList.contains("invisible") && el.classList.contains("pointer-events-none")) {
                // Deliberately-hidden UI (dropdown/menu panel) - leave it alone,
                // setupDropdowns() below owns showing/hiding these.
                return;
            }
            if (el.hasAttribute("data-hero-fallback")) {
                // The hero <img> behind the hero <video> - it's meant to stay
                // hidden while the video plays; setupHeroVideoFallback() below
                // is the only thing allowed to reveal it (if the video fails).
                return;
            }
            // Defer to next frame so the CSS transition on .opacity-0 (site.css) runs.
            requestAnimationFrame(function () {
                el.classList.remove("opacity-0");
                el.classList.add("opacity-100");
            });
        });
    }

    // ------------------------------------------------------------------
    // 2) Navbar: entrance animation + hide-on-scroll-down / show-on-scroll-up,
    //    matching the classes already present on [data-app-navbar].
    // ------------------------------------------------------------------
    function setupNavbar() {
        var navbar = document.querySelector("[data-app-navbar]");
        if (!navbar) return;

        requestAnimationFrame(function () {
            navbar.classList.remove("-translate-y-full", "opacity-0");
            navbar.classList.add("translate-y-0", "opacity-100");
        });

        var lastScrollY = window.scrollY;
        var ticking = false;

        window.addEventListener("scroll", function () {
            if (ticking) return;
            ticking = true;
            requestAnimationFrame(function () {
                var currentY = window.scrollY;
                if (currentY > lastScrollY && currentY > 120) {
                    navbar.classList.add("-translate-y-full");
                    navbar.classList.remove("translate-y-0");
                } else {
                    navbar.classList.remove("-translate-y-full");
                    navbar.classList.add("translate-y-0");
                }
                lastScrollY = currentY;
                ticking = false;
            });
        }, { passive: true });
    }

    // ------------------------------------------------------------------
    // 3) Dropdown / hamburger menu panels: [data-dropdown-container] and
    //    [data-navbar-menu-modal] both hold a sibling panel with
    //    "opacity-0 invisible pointer-events-none" that toggles open.
    // ------------------------------------------------------------------
    function setupDropdowns() {
        var triggers = document.querySelectorAll(
            "[data-dropdown-container], [data-navbar-menu-modal]"
        );

        function closeAll(except) {
            triggers.forEach(function (t) {
                if (t === except) return;
                var panel = t.querySelector(":scope > div:last-child");
                if (panel) hidePanel(panel);
            });
        }

        function showPanel(panel) {
            panel.classList.remove("opacity-0", "invisible", "pointer-events-none");
            panel.classList.add("opacity-100", "visible");
        }

        function hidePanel(panel) {
            panel.classList.add("opacity-0", "invisible", "pointer-events-none");
            panel.classList.remove("opacity-100", "visible");
        }

        triggers.forEach(function (trigger) {
            var panel = trigger.querySelector(":scope > div:last-child");
            if (!panel) return;

            trigger.addEventListener("click", function (e) {
                e.stopPropagation();
                var isOpen = panel.classList.contains("opacity-100");
                closeAll(trigger);
                if (isOpen) {
                    hidePanel(panel);
                } else {
                    showPanel(panel);
                }
            });
        });

        document.addEventListener("click", function () {
            closeAll(null);
        });
    }

    // ------------------------------------------------------------------
    // 4) react-fast-marquee ticker: the saved page only kept one copy of
    //    the scrolling content, so duplicate it once for a seamless loop
    //    (site.css animates .rfm-marquee from translateX(0) to -100%).
    // ------------------------------------------------------------------
    function duplicateMarquees() {
        document.querySelectorAll(".rfm-marquee").forEach(function (marquee) {
            var track = marquee.querySelector(".rfm-initial-child-container");
            if (track && marquee.children.length === 1) {
                marquee.appendChild(track.cloneNode(true));
            }
        });
    }

    // ------------------------------------------------------------------
    // 5) Hero video fallback: the hero <img> right after the hero
    //    <video> carries "opacity-0" and data-hero-fallback - it's meant
    //    to stay invisible and only appear if the video can't load (e.g.
    //    the mp4 is missing), not on every page load. revealMountAnimations()
    //    above deliberately skips it; this is the only code allowed to
    //    reveal it.
    // ------------------------------------------------------------------
    function setupHeroVideoFallback() {
        var fallbackImg = document.querySelector("[data-hero-fallback]");
        if (!fallbackImg) return;

        var heroVideo = document.querySelector("section[aria-label='Hero banner'] video");
        if (!heroVideo) return;

        function showFallback() {
            fallbackImg.classList.remove("opacity-0");
            fallbackImg.classList.add("opacity-100");
        }

        // "error" fires on the <video> itself once every <source> inside it
        // has failed (e.g. wwwroot/vid/pdybg.mp4 doesn't exist yet, or the
        // browser can't decode it).
        heroVideo.addEventListener("error", showFallback, true);

        // If the file is simply missing, load quietly fails without ever
        // reaching readyState >= 1; catch that case too.
        window.setTimeout(function () {
            if (heroVideo.readyState === 0) showFallback();
        }, 3000);
    }

    // ------------------------------------------------------------------
    // 6) Click-to-play video: the saved page kept the thumbnail + play
    //    button markup for "on-demand" video widgets (e.g. the Business
    //    Video card), but the original site only mounts the actual
    //    <video>/embed when you click it, so the save never captured a
    //    video source (see README "Known gaps"). Any element that looks
    //    like a play button - role="button" with aria-label="Play video"
    //    - and carries a data-video-src attribute gets swapped for a real
    //    <video> on click.
    //
    //    NOTE: data-video-src is currently pointed at the one real video
    //    URL this project has on hand (the hero background clip) purely
    //    as a placeholder so the interaction works end-to-end. Replace it
    //    with the actual clip for that section - open the live page,
    //    open DevTools -> Network, filter to "Media"/"mp4", click the
    //    play button there, and copy the request URL it fetches.
    // ------------------------------------------------------------------
    function setupClickToPlayVideos() {
        document.querySelectorAll('[aria-label="Play video"][data-video-src]').forEach(function (trigger) {
            trigger.addEventListener("click", function () {
                var src = trigger.getAttribute("data-video-src");
                if (!src) return;

                var video = document.createElement("video");
                video.src = src;
                video.controls = true;
                video.autoplay = true;
                video.playsInline = true;
                video.className = "absolute inset-0 w-full h-full object-cover bg-black";

                trigger.replaceWith(video);
                video.play().catch(function () {
                    // Autoplay with sound can be blocked; controls are
                    // already visible so the visitor can press play.
                });
            });
        });
    }

    // ------------------------------------------------------------------
    // 7) Visitor counter: animate the static number captured in the page
    //    up from 0 for a bit of the original page's polish.
    // ------------------------------------------------------------------
    function animateVisitorCounter() {
        document.querySelectorAll("p").forEach(function (p) {
            var match = /^Visitor Count:\s*([\d,]+)$/.exec(p.textContent.trim());
            if (!match) return;

            var target = parseInt(match[1].replace(/,/g, ""), 10);
            if (!target) return;

            var duration = 1200;
            var start = null;

            function step(timestamp) {
                if (!start) start = timestamp;
                var progress = Math.min((timestamp - start) / duration, 1);
                var value = Math.floor(progress * target);
                p.textContent = "Visitor Count: " + value.toLocaleString("en-IN");
                if (progress < 1) {
                    requestAnimationFrame(step);
                } else {
                    p.textContent = "Visitor Count: " + target.toLocaleString("en-IN");
                }
            }
            requestAnimationFrame(step);
        });
    }
})();
