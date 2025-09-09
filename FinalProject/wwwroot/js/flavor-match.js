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

    // ----- fixed option sets (ตามที่ผู้ใช้กำหนด) -----
    const FIXED_FLAVORS = ["หวาน", "เปรี้ยว", "ขม", "เค็ม", "อูมามิ"];
    const FIXED_FOODS = ["ทะเล", "เนื้อ", "ไก่", "หมู"];
    const FIXED_MOODS = ["Party", "Chill", "Celebration", "Fresh", "Sport"];

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
        const host = document.querySelector(`#chipset-${name}`);
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
