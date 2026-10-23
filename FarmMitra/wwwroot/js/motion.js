/* Farm Mitra - home page motion. No libraries. */
(function () {
    var root = document.documentElement;
    if (!root.classList.contains('motion')) return;       // reduced motion, or the safety switch fired
    window.__fmMotion = true;

    var $ = function (s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); };
    var clamp = function (v, a, b) { return Math.min(b, Math.max(a, v)); };

    /* ---------- reveal on scroll (staggered) ---------- */
    var TARGETS = '.hero-intro > *, .hero-art, .eyebrow, .section-heading, .section-sub, .section-head-row > .btn, .live-pill, ' +
                  '.bento-photo, .bento-tile, .scroll-row .product-card, .hiw-step, .about-alt-img, .feature-row, #farmer-live, .fx-card, .fx-chip-row, .role-card, .faq-item, .price-ticker';
    var nodes = $(TARGETS).filter(function (el, i, a) { return a.indexOf(el) === i; });
    var counted = false;

    function done(el, delay) {
        setTimeout(function () { el.classList.add('fm-done'); }, delay + 1400);
    }
    function reveal(el, delay) {
        el.style.setProperty('--d', delay + 'ms');
        el.classList.add('fm-in'); done(el, delay);
        if (el.classList.contains('bento-tile')) countUp(el.querySelector('.stat-num'), delay);
    }

    var io = new IntersectionObserver(function (entries) {
        var batch = entries.filter(function (e) { return e.isIntersecting; })
                           .sort(function (a, b) { return a.boundingClientRect.top - b.boundingClientRect.top || a.boundingClientRect.left - b.boundingClientRect.left; });
        batch.forEach(function (e, i) { reveal(e.target, Math.min(i, 6) * 90); io.unobserve(e.target); });
    }, { threshold: 0.14, rootMargin: '0px 0px -6% 0px' });

    // hero plays immediately on load, in order
    var heroNodes = $('.hero-intro > *, .hero-art');
    requestAnimationFrame(function () {
        heroNodes.forEach(function (el, i) { reveal(el, 120 + i * 120); });
    });
    nodes.forEach(function (el) { if (heroNodes.indexOf(el) < 0) io.observe(el); });

    /* ---------- count-up numbers ("500+", "1,000+", "10K+", "100%") ---------- */
    function countUp(el, delay) {
        if (!el || el.__counted) return; el.__counted = true;
        var m = /^([\d,\.]+)(.*)$/.exec(el.textContent.trim()); if (!m) return;
        var target = parseFloat(m[1].replace(/,/g, '')), suffix = m[2], grouped = m[1].indexOf(',') > -1;
        var start = null, dur = 1700;
        function fmt(n) { n = Math.round(n); return grouped ? n.toLocaleString('en-US') : String(n); }
        el.textContent = '0' + suffix;
        setTimeout(function () {
            requestAnimationFrame(function step(t) {
                if (start === null) start = t;
                var p = clamp((t - start) / dur, 0, 1), eased = p === 1 ? 1 : 1 - Math.pow(2, -10 * p);   // easeOutExpo
                el.textContent = fmt(target * eased) + suffix;
                if (p < 1) requestAnimationFrame(step);
            });
        }, delay + 250);
    }

    /* ---------- scroll-linked: parallax, hero fade, progress bar, navbar ---------- */
    var hero = document.querySelector('.hero'), heroArt = document.querySelector('.hero-art'),
        heroIntro = document.querySelector('.hero-intro'), navbar = document.querySelector('.navbar'),
        parallax = $('.bento-photo, .about-alt-grid');
    var bar = document.createElement('div'); bar.className = 'fm-progress'; document.body.appendChild(bar);
    if (heroIntro) setTimeout(function () { heroIntro.classList.add('fm-scroll'); }, 2200);

    var ticking = false;
    function frame() {
        ticking = false;
        var y = window.pageYOffset, vh = window.innerHeight;
        var max = Math.max(1, document.documentElement.scrollHeight - vh);
        bar.style.setProperty('--sp', clamp(y / max, 0, 1).toFixed(4));
        if (navbar) navbar.classList.toggle('is-scrolled', y > 10);

        if (hero && y < vh * 1.3) {
            var p = clamp(y / (vh * 0.9), 0, 1);
            if (heroArt) { heroArt.style.setProperty('--py', (y * 0.07).toFixed(1) + 'px'); heroArt.style.setProperty('--ps', (1 - p * 0.07).toFixed(4)); }
            if (heroIntro) { heroIntro.style.setProperty('--iy', (-y * 0.10).toFixed(1) + 'px'); heroIntro.style.setProperty('--io', (1 - p * 0.85).toFixed(3)); }
        }
        parallax.forEach(function (box) {
            var r = box.getBoundingClientRect();
            if (r.bottom < 0 || r.top > vh) return;
            var c = (r.top + r.height / 2 - vh / 2) / vh;                     // -1 .. 1 across the screen
            box.style.setProperty('--bp', (-c * 26).toFixed(1) + 'px');
            var img = box.querySelector('img') || box;
            if (box.classList.contains('about-alt-grid')) {
                var im = box.querySelector('.about-alt-img');
                if (im && im.classList.contains('fm-done')) im.style.setProperty('--ap', (-c * 22).toFixed(1) + 'px');
            }
        });
    }
    function onScroll() { if (!ticking) { ticking = true; requestAnimationFrame(frame); } }
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll);
    frame();
})();
