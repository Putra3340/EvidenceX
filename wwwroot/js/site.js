// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ===== Navbar =====
(function () {
    const navbar = document.getElementById('mainNavbar');
    if (!navbar) return;

    // Shadow halus saat halaman di-scroll
    const toggleShadow = () => navbar.classList.toggle('navbar-scrolled', window.scrollY > 8);
    toggleShadow();
    window.addEventListener('scroll', toggleShadow, { passive: true });

    // Animasi hamburger saat drawer buka/tutup
    const drawer = document.getElementById('mobileNav');
    const toggler = navbar.querySelector('.custom-toggler');
    if (drawer && toggler) {
        drawer.addEventListener('show.bs.offcanvas', () => toggler.classList.add('is-open'));
        drawer.addEventListener('hide.bs.offcanvas', () => toggler.classList.remove('is-open'));
    }

    // Tandai link halaman aktif berdasarkan URL saat ini
    document.querySelectorAll('.navbar .nav-link[href]').forEach(link => {
        if (link.getAttribute('href') === window.location.pathname) {
            link.classList.add('active');
            link.setAttribute('aria-current', 'page');
        }
    });
})();