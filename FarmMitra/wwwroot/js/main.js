// Farm Mitra — main.js
// Shared interactions for the marketing site.

document.addEventListener('DOMContentLoaded', () => {

    /* Mobile navigation toggle */
    const navToggle = document.querySelector('.nav-toggle');
    const mobileNav = document.querySelector('.mobile-nav');

    if (navToggle && mobileNav) {
        navToggle.addEventListener('click', () => {
            const isOpen = mobileNav.classList.toggle('open');
            navToggle.innerHTML = isOpen
                ? '<i class="fa-solid fa-xmark"></i>'
                : '<i class="fa-solid fa-bars"></i>';
            navToggle.setAttribute('aria-expanded', isOpen);
        });

        mobileNav.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', () => {
                mobileNav.classList.remove('open');
                navToggle.innerHTML = '<i class="fa-solid fa-bars"></i>';
                navToggle.setAttribute('aria-expanded', 'false');
            });
        });
    }

    /* Navbar elevation on scroll */
    const navbar = document.querySelector('.navbar');
    if (navbar) {
        const onScroll = () => {
            navbar.style.boxShadow = window.scrollY > 8
                ? '0 4px 20px rgba(15, 40, 18, 0.08)'
                : 'none';
        };
        onScroll();
        window.addEventListener('scroll', onScroll, { passive: true });
    }

    /* Add-to-cart micro feedback (homepage marketplace preview) */
    document.querySelectorAll('[data-add-cart]').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            const original = btn.innerHTML;
            btn.innerHTML = '<i class="fa-solid fa-check"></i><span>Added</span>';
            btn.disabled = true;
            setTimeout(() => {
                btn.innerHTML = original;
                btn.disabled = false;
            }, 1400);
        });
    });

    /* Dashboard sidebar toggle (mobile) */
    const dashToggle = document.querySelector('.dash-topbar-mobile-toggle');
    const dashSidebar = document.querySelector('.dash-sidebar');
    const dashOverlay = document.querySelector('.dash-overlay');
    if (dashToggle && dashSidebar) {
        dashToggle.addEventListener('click', () => {
            dashSidebar.classList.toggle('open');
            if (dashOverlay) dashOverlay.classList.toggle('show');
        });
    }
    if (dashOverlay) {
        dashOverlay.addEventListener('click', () => {
            dashSidebar.classList.remove('open');
            dashOverlay.classList.remove('show');
        });
    }

});
