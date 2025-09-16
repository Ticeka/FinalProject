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

    // ----- fixed option sets (ตามที่ผู้ใช้กำหนด/สำรอง) -----
    const FIXED_FLAVORS = ["หวาน", "เปรี้ยว", "ขม", "เค็ม", "อูมามิ"];
    const FIXED_FOODS = ["ทะเล", "เนื้อ", "ไก่", "หมู"];
    const FIXED_MOODS = ["Party", "Chill", "Celebration", "Fresh", "Sport"];

    // ====== SVG ไอคอนสำหรับหมวด 'อาหาร' ======
    // (ขนาดจะถูกบีบด้วย CSS .chip__img svg { width:20px;height:20px })
    const FOOD_ICON_SVGS = {
        "ทะเล": `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 12c2.5 0 3.5-2 6-2s3.5 2 6 2 3.5-2 6-2" fill="none" stroke="currentColor" stroke-width="1.5"/><path d="M4 16c1.8 0 2.7-1.5 5-1.5S12.2 16 14 16s2.7-1.5 5-1.5" fill="none" stroke="currentColor" stroke-width="1.5"/><circle cx="7" cy="10.5" r="0.8" fill="currentColor"/></svg>`,
        "ไก่": `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15 6c-1 0-2 .5-2 1.8 0 1.2 1 2.2 2.4 2.2H17a4.5 4.5 0 0 1 0 9H9.5A4.5 4.5 0 0 1 5 14V8.5" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/><circle cx="18.2" cy="5.2" r="1.2" fill="currentColor"/></svg>`,
        "เนื้อ": `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 6h10c3 0 5 2.5 5 5.5S20 17 17 17H7c-3 0-5-2.5-5-5.5S4 6 7 6Z" fill="none" stroke="currentColor" stroke-width="1.5"/><path d="M9 9.5c0-.8.7-1.5 1.5-1.5S12 8.7 12 9.5 11.3 11 10.5 11 9 10.3 9 9.5Z" fill="currentColor"/></svg>`,
        "หมู": `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 12c0-3 2.5-5.5 6-5.5h4c3.5 0 6 2.5 6 5.5v2a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3v-2Z" fill="none" stroke="currentColor" stroke-width="1.5"/><circle cx="8.5" cy="12.5" r="0.8" fill="currentColor"/><circle cx="15.5" cy="12.5" r="0.8" fill="currentColor"/></svg>`
    };

    // ---------- helpers ----------
    function iconForFood(value) {
        // คืน SVG ของหมวดอาหาร ถ้าไม่มีตรงชื่อจะวาดเป็น emoji fallback
        const svg = FOOD_ICON_SVGS[value];
        if (svg) return svg;
        return `<span style="font-size:16px">${value === "ทะเล" ? "🦐" : value === "ไก่" ? "🍗" : value === "เนื้อ" ? "🥩" : value === "หมู" ? "🐖" : "🍽"}</span>`;
    }

    function chipHtml(name, value, checked = false) {
        const safe = String(value);
        const isFood = name === "food";
        // ใส่รูปเฉพาะหมวดอาหาร
        const img = isFood ? `<span class="chip__img" aria-hidden="true">${iconForFood(safe)}</span>` : "";
        const cls = isFood ? "chip chip--withimg chip--food" : "chip";

        return `
<label class="${cls}" title="${safe}">
  <input type="checkbox" name="${name}" value="${safe}" ${checked ? "checked" : ""} />
  ${img}
  <span class="chip__text">${safe}</span>
</label>`;
    }

    function renderChipset(name, items, preset = []) {
        const host = document.querySelector(`#chipset-${name}`);
        if (!host) return;
        host.innerHTML = (items || [])
            .map(v => chipHtml(name, v, preset.includes(v)))
            .join("");
    }

    function readChecked(name) { return $all(`input[name="${name}"]:checked`).map(i => i.value); }

    function saveHistory(entry) {
        const arr = JSON.parse(localStorage.getItem(HISTORY_KEY) || "[]");
        arr.unshift({ ...entry, t: Date.now() });
        while (arr.length > 8) arr.pop();
        localStorage.setItem(HISTORY_KEY, JSON.stringify(arr));
    }
    function renderHistory() {
        const arr = JSON.parse(localStorage.getItem(HISTORY_KEY) || "[]");
        if (!histList) return;
        histList.innerHTML = arr.map(h => {
            const extra = [
                (h.foods?.length ? `🍽 ${h.foods.join(", ")}` : null),
                (h.moods?.length ? `🎭 ${h.moods.join(", ")}` : null)
            ].filter(Boolean).join(" • ");
            const dt = new Date(h.t || Date.now());
            return `
<div class="hist-item">
  <div class="hist-head">
    <div class="hist-title">${h.base} • ${(h.flavors || []).join(", ") || "Signature"}</div>
    <time class="hist-time">${dt.toLocaleString()}</time>
  </div>
  <div class="hist-sub">${extra || ""}</div>
</div>`;
        }).join("");
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

    function buildShareUrl({ base, flavors, foods, moods }) {
        const u = new URL(location.href);
        const sp = u.searchParams;
        sp.set("base", base || "");
        sp.set("flavors", (flavors || []).join(","));
        sp.set("foods", (foods || []).join(","));
        sp.set("moods", (moods || []).join(","));
        return u.toString();
    }

    async function fetchMatch(base, flavors, foods, moods) {
        const payload = { base, flavors, foods, moods, take: 6 };
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

    // ---------- load options ----------
    async function loadOptionsAndRender() {
        try {
            const base = baseEl.value || "";
            const { flavors = [], foods = [], moods = [] } = await fetchOptions(base);
            renderChipset("flavor", flavors.length ? flavors : FIXED_FLAVORS, dl.flavors);
            renderChipset("food", foods.length ? foods : FIXED_FOODS, dl.foods);
            renderChipset("mood", moods.length ? moods : FIXED_MOODS, dl.moods);
        } catch (e) {
            console.error("load options failed", e);
            // fallback: ใช้ชุดตายตัว
            renderChipset("flavor", FIXED_FLAVORS, dl.flavors);
            renderChipset("food", FIXED_FOODS, dl.foods);
            renderChipset("mood", FIXED_MOODS, dl.moods);
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

        recTitle.textContent = "กำลังประมวลผล…";
        recSub.textContent = "จะใช้คะแนนช่วยหาเฉพาะเมื่อยังไม่เจอคำตอบตรง ๆ";
        resultWrap.classList.add("d-none");

        try {
            const out = await fetchMatch(base, flavors, foods, moods);
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

            saveHistory({ base, flavors, foods, moods });
            renderHistory();
        } catch (err) {
            alert(err?.message || "เกิดข้อผิดพลาด");
            console.error(err);
        }
    });

    // ---------- reset ----------
    $("#btnReset")?.addEventListener("click", () => {
        $all('input[type="checkbox"]').forEach(i => i.checked = false);
        resultWrap.innerHTML = "";
        resultWrap.classList.add("d-none");
        recTitle.textContent = "ยังไม่มีคำแนะนำ";
        recSub.textContent = "เลือกตัวกรองด้านซ้ายแล้วกด “แนะนำเลย”";
    });

    // ---------- clear history ----------
    $("#btnClearHist")?.addEventListener("click", () => {
        localStorage.removeItem(HISTORY_KEY);
        renderHistory();
    });

    // ---------- share ----------
    btnShare?.addEventListener("click", async () => {
        const base = baseEl.value;
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
