/* =========================================
   สคริปต์ควบคุมการแสดงผลแถบนำทาง (ภาษาไทย)
   - เพิ่มคลาส .is-scrolled เมื่อเลื่อนหน้าลง
   - ช่วยให้แถบดูทึบ/มีเงาชัดขึ้น ไม่กลืนกับขอบเบราว์เซอร์
   - เมื่อเปิดเมนูบนมือถือ: บังคับให้ทึบไว้เพื่อความชัดเจน
========================================= */
(function () {
    const navbar = document.querySelector('.app-navbar');
    if (!navbar) return;

    const onScroll = () => {
        if (window.scrollY > 8) {
            navbar.classList.add('is-scrolled');
        } else {
            navbar.classList.remove('is-scrolled');
        }
    };

    // เรียกครั้งแรก + ผูกเหตุการณ์เลื่อน
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });

    // เมื่อเมนูมือถือถูกเปิด/ปิด ให้ปรับสถานะคลาส
    const toggler = document.querySelector('[data-bs-toggle="collapse"][data-bs-target="#navbarNav"]');
    const navCollapse = document.getElementById('navbarNav');
    if (toggler && navCollapse) {
        navCollapse.addEventListener('shown.bs.collapse', () => navbar.classList.add('is-scrolled'));
        navCollapse.addEventListener('hidden.bs.collapse', onScroll);
    }
})();
