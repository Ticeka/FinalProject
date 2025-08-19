(function () {
    const $ = (s, p = document) => p.querySelector(s);
    const $all = (s, p = document) => [...p.querySelectorAll(s)];

    const form = $("#matchForm");
    const baseEl = $("#base");
    const resultWrap = $("#resultWrap");
    const recTitle = $("#recTitle");
    const recSub = $("#recSub");
    const histList = $("#histList");
    const btnShare = $("#btnShare");
    const shareToast = $("#shareToast");

    const API_MATCH = "/api/reco/flavor-match";
    const API_OPTS = "/api/reco/flavor-options";
    const HISTORY_KEY = "flavor_match_hist";

    // ---------- helpers ----------
    function chipHtml(name, value, checked = false) {
        const safe = String(value);
        return `
      <label class="chip" title="${safe}">
        <input type="checkbox" name="${name}" value="${safe}" ${checked ? "checked" : ""} />
        <span>${safe}</span>
      </label>`;
    }
    function renderChipset(name, items, preset = []) {
        const host = $(`#chipset-${name}`);
        if (!host) return;
        host.innerHTML = (items || [])
            .map(v => chipHtml(name, v, preset.includes(v)))
            .join("");
    }
    function readChecked(name) {
        return $all(`input[name="${name}"]:checked`).map(i => i.value);
    }
    function saveHistory(entry) {
        const arr = JSON.parse(localStorage.getItem(HISTORY_KEY) || "[]");
        arr.unshift({ ...entry, t: Date.now() });
        while (arr.length > 8) arr.pop();
        localStorage.setItem(HISTORY_KEY, JSON.stringify(arr));
    }
    function renderHistory() {
        const arr = JSON.parse(localStorage.getItem(HISTORY_KEY) || "[]");
        histList.innerHTML = arr.map(it => `
      <div class="hist-item">
        <div class="t">${it.title}</div>
        <div class="s">
          ${it.base || "—"}
          ${it.flavors?.length ? " • " + it.flavors.join(", ") : ""}
          ${it.foods?.length ? " • 🍽 " + it.foods.join(", ") : ""}
          ${it.moods?.length ? " • 🎭 " + it.moods.join(", ") : ""}
        </div>
      </div>
    `).join("");
    }
    function buildShareUrl({ base, flavors, foods, moods }) {
        const u = new URL(location.href);
        if (base) u.searchParams.set("base", base);
        if (flavors?.length) u.searchParams.set("flavors", flavors.join(","));
        if (foods?.length) u.searchParams.set("foods", foods.join(","));
        if (moods?.length) u.searchParams.set("moods", moods.join(","));
        return u.toString();
    }
    async function fetchMatch(base, flavors) {
        const payload = { base, flavors, take: 6 }; // backend ตอนนี้รับเท่านี้
        const res = await fetch(API_MATCH, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload),
            credentials: "same-origin"
        });
        if (!res.ok) throw new Error(await res.text());
        return res.json();
    }
    async function fetchOptions(base) {
        const url = new URL(API_OPTS, location.origin);
        if (base) url.searchParams.set("base", base);
        const res = await fetch(url, { credentials: "same-origin" });
        if (!res.ok) throw new Error(await res.text());
        return res.json(); // { flavors:[], foods:[], moods:[] }
    }

    // ---------- deep link ----------
    const sp = new URLSearchParams(location.search);
    const dl = {
        base: sp.get("base") || "",
        flavors: (sp.get("flavors") || "").split(",").map(s => s.trim()).filter(Boolean),
        foods: (sp.get("foods") || "").split(",").map(s => s.trim()).filter(Boolean),
        moods: (sp.get("moods") || "").split(",").map(s => s.trim()).filter(Boolean),
    };
    if (dl.base) baseEl.value = dl.base;

    // ---------- load options when base changes ----------
    async function loadOptionsAndRender() {
        try {
            const base = baseEl.value || "";
            const { flavors = [], foods = [], moods = [] } = await fetchOptions(base);
            renderChipset("flavor", flavors, dl.flavors);
            renderChipset("food", foods, dl.foods);
            renderChipset("mood", moods, dl.moods);
        } catch (e) {
            console.error("load options failed", e);
            // fallback minimal (ถ้าอยากมี default)
            renderChipset("flavor", ["Citrus", "Herb", "Sweet", "Bitter", "Smoke", "Spice", "Malty", "Hoppy", "Fruity", "Floral", "Woody"], dl.flavors);
            renderChipset("food", ["ลาบหมูคั่ว", "หมูแดดเดียว", "ชีสเค้ก", "ซูชิ"], dl.foods);
            renderChipset("mood", ["Chill", "Party", "Celebration", "Romantic"], dl.moods);
        } finally {
            // ใช้ deep-link รอบแรกครั้งเดียวพอ
            dl.flavors = []; dl.foods = []; dl.moods = [];
        }
    }
    baseEl.addEventListener("change", loadOptionsAndRender);

    // ---------- submit ----------
    form?.addEventListener("submit", async (e) => {
        e.preventDefault();
        const base = baseEl.value;
        const flavors = readChecked("flavor");
        const foods = readChecked("food");
        const moods = readChecked("mood");

        if (!base) { alert("กรุณาเลือกฐานเครื่องดื่ม"); return; }

        recTitle.textContent = "กำลังผสมรสที่ใช่...";
        recSub.textContent = "กำลังค้นหาที่เข้ากับรสชาติและคะแนนรีวิว";
        resultWrap.classList.add("d-none");

        try {
            const out = await fetchMatch(base, flavors);
            recTitle.textContent = `${out.base} • ${out.flavors.join(", ") || "Signature"}`;
            const extra = [
                foods.length ? `🍽 ${foods.join(", ")}` : null,
                moods.length ? `🎭 ${moods.join(", ")}` : null
            ].filter(Boolean).join(" • ");
            recSub.textContent = `พบคำแนะนำ ${out.items.length} รายการ${extra ? " • " + extra : ""}`;

            resultWrap.innerHTML = out.items.map(x => {
                const img = x.imageUrl || "https://via.placeholder.com/640x480?text=Sip+%26+Trip";
                const price = (typeof x.price === "number" && x.price > 0) ? ` • ฿${x.price.toLocaleString()}` : "";
                const rating = x.rating ? `⭐ ${x.rating.toFixed(1)} (${x.ratingCount || 0})` : "";
                const meta = [x.type, x.province].filter(Boolean).join(" • ");
                return `
          <div class="col">
            <div class="reco-card">
              <img class="reco-img" src="${img}" alt="${x.name}">
              <div class="reco-body">
                <div class="d-flex justify-content-between align-items-center mb-1">
                  <div class="reco-name">${x.name}</div>
                  <span class="reco-badge">Score ${x.score?.toFixed?.(2) ?? "—"}</span>
                </div>
                <div class="reco-meta">${meta}${price}</div>
                ${rating ? `<div class="reco-meta mt-1">${rating}</div>` : ``}
                <div class="reco-why">${x.why || ""}</div>
                ${x.id ? `<a class="btn btn-sm btn-soft mt-2" href="/Detail?id=${x.id}">ดูข้อมูล</a>` : ``}
              </div>
            </div>
          </div>`;
            }).join("");
            resultWrap.classList.remove("d-none");

            // history + deep link
            saveHistory({ title: recTitle.textContent, base: out.base, flavors: out.flavors, foods, moods });
            renderHistory();
            history.replaceState(null, "", buildShareUrl({ base, flavors, foods, moods }));

        } catch (err) {
            console.error(err);
            recTitle.textContent = "ขออภัย แนะนำไม่สำเร็จ";
            recSub.textContent = "ลองใหม่อีกครั้ง หรือตรวจสอบการเชื่อมต่อ";
            resultWrap.classList.add("d-none");
        }
    });

    // ---------- share ----------
    btnShare?.addEventListener("click", async () => {
        const base = baseEl.value || "";
        const flavors = readChecked("flavor");
        const foods = readChecked("food");
        const moods = readChecked("mood");
        const url = buildShareUrl({ base, flavors, foods, moods });
        try {
            await navigator.clipboard.writeText(url);
            shareToast?.classList.remove("d-none");
            setTimeout(() => shareToast?.classList.add("d-none"), 1500);
        } catch { alert("คัดลอกลิงก์ไม่สำเร็จ"); }
    });

    // ---------- init ----------
    renderHistory();
    loadOptionsAndRender(); // โหลด options ครั้งแรกตาม base (ถ้ามีใน query)
})();
