// ปุ่มช่วงเวลาเร็ว
(function () {
    const btns = document.querySelectorAll('[data-range]');
    if (!btns.length) return;

    const $ = id => document.getElementById(id);
    function setDateInput(id, d) {
        const el = $(id); if (!el) return;
        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        el.value = `${yyyy}-${mm}-${dd}`;
    }

    btns.forEach(b => {
        b.addEventListener('click', () => {
            const range = b.getAttribute('data-range');
            const now = new Date();
            const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

            if (range === 'today') {
                setDateInput('dateFrom', today);
                setDateInput('dateTo', today);
            } else if (range === '7d') {
                const from = new Date(today); from.setDate(from.getDate() - 6);
                setDateInput('dateFrom', from);
                setDateInput('dateTo', today);
            } else if (range === '30d') {
                const from = new Date(today); from.setDate(from.getDate() - 29);
                setDateInput('dateFrom', from);
                setDateInput('dateTo', today);
            } else if (range === 'all') {
                const df = $('dateFrom'); const dt = $('dateTo');
                if (df) df.value = ""; if (dt) dt.value = "";
            }
        });
    });
})();
