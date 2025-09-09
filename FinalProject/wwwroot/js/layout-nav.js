// เงา Navbar เมื่อเลื่อนหน้า + ปรับโหมด once-on-load
(function () {
    const nav = document.querySelector('.navbar');
    if (!nav) return;

    const toggle = () => {
        if (window.scrollY > 6) nav.classList.add('is-scrolled');
        else nav.classList.remove('is-scrolled');
    };

    // เรียกครั้งแรก และผูก event
    toggle();
    window.addEventListener('scroll', toggle, { passive: true });
})();
