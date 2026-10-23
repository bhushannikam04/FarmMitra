/* Farm Mitra - language picker (22 Indian languages + English).
   Uses Google's free website translator, but ONLY loads it after the visitor picks a language,
   so nothing is sent to Google while the site is in English. */
(function () {
    // code = Google Translate code, native = name in its own script
    var LANGS = [
        { code: 'en',       native: 'English',        name: 'English' },
        { code: 'hi',       native: 'हिन्दी',          name: 'Hindi' },
        { code: 'mr',       native: 'मराठी',           name: 'Marathi' },
        { code: 'gu',       native: 'ગુજરાતી',         name: 'Gujarati' },
        { code: 'bn',       native: 'বাংলা',           name: 'Bengali' },
        { code: 'pa',       native: 'ਪੰਜਾਬੀ',          name: 'Punjabi' },
        { code: 'ta',       native: 'தமிழ்',           name: 'Tamil' },
        { code: 'te',       native: 'తెలుగు',          name: 'Telugu' },
        { code: 'kn',       native: 'ಕನ್ನಡ',           name: 'Kannada' },
        { code: 'ml',       native: 'മലയാളം',          name: 'Malayalam' },
        { code: 'or',       native: 'ଓଡ଼ିଆ',           name: 'Odia' },
        { code: 'as',       native: 'অসমীয়া',         name: 'Assamese' },
        { code: 'ur',       native: 'اردو',            name: 'Urdu' },
        { code: 'sa',       native: 'संस्कृतम्',        name: 'Sanskrit' },
        { code: 'gom',      native: 'कोंकणी',          name: 'Konkani' },
        { code: 'ks',       native: 'کٲشُر',            name: 'Kashmiri' },
        { code: 'sd',       native: 'سنڌي',            name: 'Sindhi' },
        { code: 'ne',       native: 'नेपाली',          name: 'Nepali' },
        { code: 'doi',      native: 'डोगरी',           name: 'Dogri' },
        { code: 'mai',      native: 'मैथिली',          name: 'Maithili' },
        { code: 'mni-Mtei', native: 'ꯃꯩꯇꯩꯂꯣꯟ',        name: 'Manipuri (Meitei)' },
        { code: 'brx',      native: 'बड़ो',            name: 'Bodo' },
        { code: 'sat',      native: 'ᱥᱟᱱᱛᱟᱲᱤ',         name: 'Santali' }
    ];
    var KEY = 'fm-lang', root = document.documentElement;

    function byCode(c) { for (var i = 0; i < LANGS.length; i++) if (LANGS[i].code === c) return LANGS[i]; return LANGS[0]; }

    // ---- the cookie Google reads: googtrans=/en/<code> ----
    function cookieLang() {
        var m = document.cookie.match(/(?:^|;\s*)googtrans=\/[^\/]*\/([^;]+)/);
        var c = m ? decodeURIComponent(m[1]) : 'en';
        return byCode(c).code;
    }
    function writeCookie(code) {
        var host = location.hostname, parts = host.split('.'), domains = ['', host];
        if (parts.length > 1) domains.push('.' + host);
        domains.forEach(function (d) {
            var base = 'googtrans=' + (code === 'en' ? '' : '/en/' + code) + '; path=/' + (d ? '; domain=' + d : '');
            document.cookie = code === 'en' ? base + '; expires=Thu, 01 Jan 1970 00:00:00 GMT' : base;
        });
    }
    function saved() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
    function remember(code) { try { localStorage.setItem(KEY, code); } catch (e) { } }

    // ---- protect text that must never be translated (brand name) ----
    function protect() {
        document.querySelectorAll('.brand, .notranslate').forEach(function (el) {
            el.setAttribute('translate', 'no'); el.classList.add('notranslate');
        });
    }

    // ---- load Google's translator (only when a language other than English is active) ----
    var loaded = false;
    window.fmTranslateInit = function () {
        try {
            new google.translate.TranslateElement({
                pageLanguage: 'en',
                includedLanguages: LANGS.filter(function (l) { return l.code !== 'en'; }).map(function (l) { return l.code; }).join(','),
                autoDisplay: false
            }, 'fm_translate_host');
        } catch (e) { fail(); }
    };
    function loadGoogle(code) {
        if (loaded) return; loaded = true;
        var host = document.createElement('div');
        host.id = 'fm_translate_host'; host.className = 'notranslate';
        host.style.cssText = 'position:absolute;left:-9999px;top:0;width:1px;height:1px;overflow:hidden;';
        document.body.appendChild(host);
        var s = document.createElement('script');
        s.src = 'https://translate.google.com/translate_a/element.js?cb=fmTranslateInit';
        s.async = true; s.onerror = fail;
        document.head.appendChild(s);
        // If nothing happened after a few seconds, tell the user instead of silently staying in English.
        setTimeout(function () {
            if (!/translated-(ltr|rtl)/.test(root.className)) fail();
        }, 6000);
    }

    function toast(msg) {
        var t = document.createElement('div');
        t.className = 'lang-toast'; t.setAttribute('role', 'status'); t.textContent = msg;
        document.body.appendChild(t);
        setTimeout(function () { t.classList.add('show'); }, 20);
        setTimeout(function () { t.classList.remove('show'); setTimeout(function () { t.remove(); }, 400); }, 6000);
    }
    var failed = false;
    function fail() {
        if (failed) return; failed = true;
        var l = byCode(cookieLang());
        if (l.code === 'en') return;
        writeCookie('en'); remember('en');
        toast('Sorry, ' + l.name + ' could not be loaded on this page (the language may not be offered for websites yet, or you are offline). Showing English.');
        setLabel('en');
    }

    // ---- picker UI ----
    function setLabel(code) {
        var l = byCode(code);
        document.querySelectorAll('[data-lang-current]').forEach(function (el) { el.textContent = l.code === 'en' ? 'English' : l.native; });
        document.querySelectorAll('.lang-item').forEach(function (li) {
            var on = li.getAttribute('data-code') === code;
            li.classList.toggle('active', on); li.setAttribute('aria-checked', on);
        });
        root.setAttribute('data-lang', code);
    }
    function build(box) {
        var menu = box.querySelector('.lang-menu');
        LANGS.forEach(function (l) {
            var b = document.createElement('button');
            b.type = 'button'; b.className = 'lang-item notranslate'; b.setAttribute('role', 'menuitemradio');
            b.setAttribute('data-code', l.code); b.setAttribute('translate', 'no');
            b.innerHTML = '<span class="lang-native"></span><span class="lang-name"></span>';
            b.firstChild.textContent = l.native; b.lastChild.textContent = l.name;
            menu.appendChild(b);
        });
    }
    function closeAll() {
        document.querySelectorAll('.lang-picker.open').forEach(function (b) {
            b.classList.remove('open'); b.querySelector('.lang-btn').setAttribute('aria-expanded', 'false');
        });
    }
    function choose(code) {
        if (code === cookieLang()) { closeAll(); return; }
        writeCookie(code); remember(code);
        location.reload();             // Google reads the cookie when the page loads
    }

    document.addEventListener('DOMContentLoaded', function () {
        protect();
        document.querySelectorAll('.lang-picker').forEach(build);
        var cur = cookieLang();
        // the cookie is the truth; if it was cleared but a choice was saved, restore it
        if (cur === 'en' && saved() && saved() !== 'en') { cur = byCode(saved()).code; writeCookie(cur); location.reload(); return; }
        setLabel(cur);
        if (cur !== 'en') loadGoogle(cur);
    });

    document.addEventListener('click', function (e) {
        var btn = e.target.closest && e.target.closest('.lang-btn');
        if (btn) {
            var box = btn.closest('.lang-picker'), open = !box.classList.contains('open');
            closeAll();
            if (open) { box.classList.add('open'); btn.setAttribute('aria-expanded', 'true'); }
            return;
        }
        var item = e.target.closest && e.target.closest('.lang-item');
        if (item) { choose(item.getAttribute('data-code')); return; }
        closeAll();
    });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeAll(); });
})();
