(function () {
    // อ่าน JSON จาก script tag
    function readJson(id) {
        const el = document.getElementById(id);
        if (!el) return [];
        try { return JSON.parse(el.textContent || "[]"); }
        catch { return []; }
    }

    const ratings = readJson("ratings-data");       // [{ day, count, avg }]
    const provinces = readJson("provinces-data");   // [{ province, count }]

    // แปลงวันที่ให้สวย
    const ratingsLabels = ratings.map(p => new Date(p.day).toLocaleDateString());
    const ratingsCount = ratings.map(p => p.count);
    const ratingsAvg = ratings.map(p => p.avg);

    // Line: Ratings over time
    const lineCtx = document.getElementById("ratingsLine");
    if (lineCtx) {
        new Chart(lineCtx, {
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
                plugins: {
                    legend: { display: true },
                    tooltip: { enabled: true }
                },
                scales: {
                    y: { beginAtZero: true, ticks: { precision: 0 } },
                    y1: { position: "right", min: 0, max: 5, grid: { drawOnChartArea: false } }
                }
            }
        });
    }

    // Bar: Beers by Province
    const barCtx = document.getElementById("provinceBar");
    if (barCtx) {
        const labels = provinces.map(p => p.province || "N/A");
        const counts = provinces.map(p => p.count);
        new Chart(barCtx, {
            type: "bar",
            data: { labels, datasets: [{ label: "Beers", data: counts }] },
            options: {
                indexAxis: labels.length > 8 ? "y" : "x",
                responsive: true,
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: true }
                },
                scales: {
                    x: { ticks: { autoSkip: true, maxRotation: 0 } },
                    y: { beginAtZero: true, ticks: { precision: 0 } }
                }
            }
        });
    }
})();
