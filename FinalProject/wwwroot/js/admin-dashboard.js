// /wwwroot/js/admin-dashboard.js
(function () {
    // -------- Utils --------
    function readJson(id) {
        const el = document.getElementById(id);
        if (!el) return [];
        try { return JSON.parse(el.textContent || "[]"); }
        catch { return []; }
    }
    const pick = (o, a, b) => (o && (o[a] ?? o[b])) ?? null;
    const toNum = (v, def = 0) => {
        const n = Number(v);
        return Number.isFinite(n) ? n : def;
    };
    const toDate = (v) => {
        if (!v) return null;
        const d = new Date(v);
        return isNaN(d) ? null : d;
    };
    const TH_LOCALE = document.documentElement.lang || "th-TH";

    // ทำลายกราฟเก่าหากมี (ป้องกันซ้อน)
    function destroyIfAny(canvas) {
        if (!canvas) return;
        if (canvas.__chart) {
            try { canvas.__chart.destroy(); } catch { /* ignore */ }
            canvas.__chart = null;
        }
    }

    // -------- Read data from page --------
    const ratingsRaw = readJson("ratings-data");     // [{ Day/day, Count/count, Avg/avg }]
    const provincesRaw = readJson("provinces-data");   // [{ Province/province, Count/count }]

    // -------- Prepare Ratings dataset --------
    const ratings = (Array.isArray(ratingsRaw) ? ratingsRaw : [])
        .map(p => ({
            day: toDate(pick(p, "day", "Day")),
            count: toNum(pick(p, "count", "Count")),
            avg: toNum(pick(p, "avg", "Avg"))
        }))
        .filter(x => !!x.day)
        .sort((a, b) => a.day - b.day);

    const ratingsLabels = ratings.map(p => p.day.toLocaleDateString(TH_LOCALE, { day: "2-digit", month: "short" }));
    const ratingsCount = ratings.map(p => p.count);
    const ratingsAvg = ratings.map(p => p.avg);

    // -------- Prepare Provinces dataset --------
    const provinces = (Array.isArray(provincesRaw) ? provincesRaw : [])
        .map(p => ({
            province: pick(p, "province", "Province") || "N/A",
            count: toNum(pick(p, "count", "Count"))
        }))
        .sort((a, b) => b.count - a.count);

    const provinceLabels = provinces.map(p => p.province);
    const provinceCounts = provinces.map(p => p.count);

    // -------- Render Charts --------
    // Ratings (last 30 days)
    const lineCtx = document.getElementById("ratingsLine");
    if (lineCtx && ratings.length) {
        destroyIfAny(lineCtx);
        const maxAvg = Math.max(5, Math.ceil(Math.max(...ratingsAvg, 0)));
        lineCtx.__chart = new Chart(lineCtx, {
            type: "line",
            data: {
                labels: ratingsLabels,
                datasets: [
                    { label: "Ratings count", data: ratingsCount, tension: 0.35 },
                    { label: "Average score", data: ratingsAvg, tension: 0.35, yAxisID: "y1" }
                ]
            },
            options: {
                responsive: true,
                interaction: { mode: "index", intersect: false },
                plugins: { legend: { display: true }, tooltip: { enabled: true } },
                scales: {
                    y: { beginAtZero: true, ticks: { precision: 0 } },
                    y1: { position: "right", min: 0, max: 5, grid: { drawOnChartArea: false } }
                }
            }
        });
    }

    // Beers by Province
    const barCtx = document.getElementById("provinceBar");
    if (barCtx && provinces.length) {
        destroyIfAny(barCtx);
        barCtx.__chart = new Chart(barCtx, {
            type: "bar",
            data: { labels: provinceLabels, datasets: [{ label: "Beers", data: provinceCounts }] },
            options: {
                indexAxis: provinceLabels.length > 8 ? "y" : "x",
                responsive: true,
                plugins: { legend: { display: false }, tooltip: { enabled: true } },
                scales: {
                    x: { ticks: { autoSkip: true, maxRotation: 0 } },
                    y: { beginAtZero: true, ticks: { precision: 0 } }
                }
            }
        });
    }

    // -------- Nice UX: ถ้ามี #lists ให้เลื่อนนุ่ม ๆ ไปยัง block รายการ --------
    if (location.hash === "#lists") {
        const el = document.getElementById("lists");
        if (el) setTimeout(() => el.scrollIntoView({ behavior: "smooth", block: "start" }), 50);
    }
})();
