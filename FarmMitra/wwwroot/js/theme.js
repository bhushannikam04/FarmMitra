/* Farm Mitra - theme switcher.
   Loaded in <head> (no defer) so the right theme is applied before the page paints. */
(function () {
    var KEY = 'fm-theme', root = document.documentElement;

    function stored() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
    function systemPrefersDark() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
    function current() { return root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light'; }

    function apply(theme) {
        root.setAttribute('data-theme', theme);
        var meta = document.querySelector('meta[name="theme-color"]');
        if (meta) meta.setAttribute('content', theme === 'dark' ? '#0E1411' : '#1B5E20');
        document.querySelectorAll('[data-theme-toggle]').forEach(function (b) {
            b.setAttribute('aria-pressed', theme === 'dark');
            b.setAttribute('aria-label', theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme');
            b.title = theme === 'dark' ? 'Light theme' : 'Dark theme';
        });
    }

    // 1) first paint: saved choice, else the device setting
    apply(stored() || (systemPrefersDark() ? 'dark' : 'light'));

    // 2) buttons
    document.addEventListener('click', function (e) {
        var btn = e.target.closest && e.target.closest('[data-theme-toggle]');
        if (!btn) return;
        var next = current() === 'dark' ? 'light' : 'dark';
        root.classList.add('theme-anim');
        apply(next);
        try { localStorage.setItem(KEY, next); } catch (e2) { }
        setTimeout(function () { root.classList.remove('theme-anim'); }, 350);
    });

    // 3) labels need the buttons to exist
    document.addEventListener('DOMContentLoaded', function () { apply(current()); });

    // 4) follow the device when the user has not chosen
    if (window.matchMedia) {
        var mq = window.matchMedia('(prefers-color-scheme: dark)');
        var onChange = function (ev) { if (!stored()) apply(ev.matches ? 'dark' : 'light'); };
        if (mq.addEventListener) mq.addEventListener('change', onChange);
    }
})();
