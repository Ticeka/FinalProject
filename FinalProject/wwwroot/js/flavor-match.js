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

    // ===== Fixed options =====
    const FIXED_FLAVORS = ["หวาน", "เปรี้ยว", "ขม", "เค็ม", "อูมามิ"];
    const FIXED_FOODS = ["ทะเล", "เนื้อ", "ไก่", "หมู"];
    const FIXED_MOODS = ["Party", "Chill", "Celebration", "Fresh", "Sport"];

    // ===== Labels (ไทย) =====
    const FOOD_LABEL_TH = {
        "ทะเล": "อาหารทะเล",
        "เนื้อ": "อาหารประเภทเนื้อ",
        "ไก่": "อาหารประเภทไก่",
        "หมู": "อาหารประเภทหมู",
    };
    const MOOD_LABEL_TH = {
        "Party": "ปาร์ตี้",
        "Chill": "ชิล",
        "Celebration": "ฉลอง",
        "Fresh": "สดชื่น",
        "Sport": "กีฬา",
    };

    // ===== Icons =====
    const FLAVOR_EMOJI = { "หวาน": "🍯", "เปรี้ยว": "🍋", "ขม": "🍫", "เค็ม": "🧂", "อูมามิ": "🍄" };
    const MOOD_EMOJI = { "Party": "🎉", "Chill": "🧘", "Celebration": "🥂", "Fresh": "🍃", "Sport": "⚽" };

    // ==== Food icons สดใส ====
    const FOOD_ICON_THEME = {
        "ทะเล": { emoji: "🐟", bg: "#e0f2fe", border: "#93c5fd" },
        "เนื้อ": { emoji: "🥩", bg: "#fee2e2", border: "#fca5a5" },
        "ไก่": { emoji: "🍗", bg: "#fff7ed", border: "#fdba74" },
        "หมู": { emoji: "🐷", bg: "#fce7f3", border: "#f9a8d4" }
    };
    function iconForFood(value) {
        const cfg = FOOD_ICON_THEME[value];
        if (!cfg) return "";
        return `
      <span class="fm-chip__img" aria-hidden="true"
            style="background:${cfg.bg}; border-color:${cfg.border};">
        ${cfg.emoji}
      </span>`;
    }

    // ===== Helpers =====
    function displayLabel(name, value) {
        if (name === "food") return FOOD_LABEL_TH[value] || value;
        if (name === "mood") return MOOD_LABEL_TH[value] || value;
        return value;
    }
    function iconFor(name, value) {
        if (name === "food") return iconForFood(value);
        if (name === "flavor") {
            const e = FLAVOR_EMOJI[value];
            return e ? `<span class="fm-chip__img" aria-hidden="true">${e}</span>` : "";
        }
        if (name === "mood") {
            const e = MOOD_EMOJI[value];
            return e ? `<span class="fm-chip__img" aria-hidden="true">${e}</span>` : "";
        }
        return "";
    }

    // ===== Chip HTML =====
    function chipHtml(name, value, checked = false) {
        const label = displayLabel(name, value);
        const icon = iconFor(name, value);
        return `
<label class="fm-chip${checked ? " is-checked" : ""}" title="${label}">
  <input type="checkbox" name="${name}" value="${value}" ${checked ? "checked" : ""} />
  ${icon}
  <span class="fm-chip__text">${label}</span>
  <span class="fm-chip__tick">✓</span>
</label>`;
    }

    function renderChipset(name, items, preset = []) {
        const host = document.querySelector(`#chipset-${name}`);
        if (!host) return;
        host.innerHTML = (items || [])
            .map((v) => chipHtml(name, v, preset.includes(v)))
            .join("");

        host.addEventListener("change", (e) => {
            if (e.target && e.target.matches('input[type="checkbox"]')) {
                const label = e.target.closest("label.fm-chip");
                if (label) {
                    label.classList.toggle("is-checked", e.target.checked);
                }
            }
        });
        host.querySelectorAll('input[type="checkbox"]').forEach((i) => {
            i.closest("label.fm-chip")?.classList.toggle("is-checked", i.checked);
        });
    }

    // ===== Deep link =====
    const sp = new URLSearchParams(location.search);
    const dl = {
        base: sp.get("base") || "",
        flavors: (sp.get("flavors") || "").split(",").map((s) => s.trim()).filter(Boolean),
        foods: (sp.get("foods") || "").split(",").map((s) => s.trim()).filter(Boolean),
        moods: (sp.get("moods") || "").split(",").map((s) => s.trim()).filter(Boolean),
    };
    if (dl.base) $("#base").value = dl.base;

    // ===== History =====
    function saveHistory(entry) {
        const arr = JSON.parse(localStorage.getItem(HISTORY_KEY) || "[]");
        arr.unshift({ ...entry, t: Date.now() });
        while (arr.length > 8) arr.pop();
        localStorage.setItem(HISTORY_KEY, JSON.stringify(arr));
    }
    function renderHistory() {
        const arr = JSON.parse(localStorage.getItem(HISTORY_KEY) || "[]");
        if (!histList) return;
        histList.innerHTML = arr.map((h) => {
            const extra = [
                h.foods?.length ? `ประเภทอาหาร: ${h.foods.map((v) => FOOD_LABEL_TH[v] || v).join(", ")}` : null,
                h.moods?.length ? `บรรยากาศ: ${h.moods.map((v) => MOOD_LABEL_TH[v] || v).join(", ")}` : null,
            ].filter(Boolean).join(" • ");
            const dt = new Date(h.t || Date.now());
            return `
<div class="fm-hist-item">
  <div class="fm-hist-head">
    <div class="fm-hist-title">${h.base || "—"} • ${(h.flavors || []).join(", ") || "Signature"}</div>
    <time class="fm-hist-time">${dt.toLocaleString()}</time>
  </div>
  <div class="fm-hist-sub">${extra || ""}</div>
</div>`;
        }).join("");
    }

    // ===== Share URL =====
    function buildShareUrl({ base, flavors, foods, moods }) {
        const u = new URL(location.href);
        const p = u.searchParams;
        p.set("base", base || "");
        p.set("flavors", (flavors || []).join(","));
        p.set("foods", (foods || []).join(","));
        p.set("moods", (moods || []).join(","));
        return u.toString();
    }

    // ===== API =====
    async function fetchMatch(base, flavors, foods, moods) {
        const res = await fetch("/api/reco/flavor-match", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ base, flavors, foods, moods, take: 6 }),
            credentials: "same-origin",
        });
        if (!res.ok) throw new Error(await res.text());
        return res.json();
    }
    async function fetchOptions(base) {
        const url = new URL("/api/reco/flavor-options", location.origin);
        if (base) url.searchParams.set("base", base);
        const res = await fetch(url, { credentials: "same-origin" });
        if (!res.ok) throw new Error(await res.text());
        return res.json();
    }

    // ===== Skeleton (ใช้กับ .fm-results) =====
    function showSkeletons(n = 6) {
        resultWrap.classList.remove("d-none");
        resultWrap.innerHTML = Array.from({ length: n }).map(() => `
<div class="fm-result">
  <div class="skel">
    <div class="ph"></div>
    <div class="bx">
      <div class="ln"></div>
      <div class="ln sm"></div>
    </div>
  </div>
</div>`).join("");
    }
    function clearResults() {
        resultWrap.innerHTML = "";
        resultWrap.classList.add("d-none");
    }

    // ===== Load options =====
    async function loadOptionsAndRender() {
        try {
            const base = baseEl.value || "";
            const { flavors = [], foods = [], moods = [] } = await fetchOptions(base);
            renderChipset("flavor", flavors.length ? flavors : FIXED_FLAVORS, dl.flavors);
            renderChipset("food", foods.length ? foods : FIXED_FOODS, dl.foods);
            renderChipset("mood", moods.length ? moods : FIXED_MOODS, dl.moods);
        } catch {
            renderChipset("flavor", FIXED_FLAVORS, dl.flavors);
            renderChipset("food", FIXED_FOODS, dl.foods);
            renderChipset("mood", FIXED_MOODS, dl.moods);
        } finally {
            dl.flavors = []; dl.foods = []; dl.moods = [];
        }
    }
    baseEl.addEventListener("change", loadOptionsAndRender);

    // ===== Submit =====
    form?.addEventListener("submit", async (e) => {
        e.preventDefault();
        const base = baseEl.value;
        const flavors = $all('input[name="flavor"]:checked').map((i) => i.value);
        const foods = $all('input[name="food"]:checked').map((i) => i.value);
        const moods = $all('input[name="mood"]:checked').map((i) => i.value);

        recTitle.textContent = "กำลังค้นหาคำแนะนำ…";
        recSub.textContent = "ถ้าไม่เจอตรง ระบบจะประเมินความใกล้เคียงให้";

        // 🔽 ซ่อน empty state
        $("#emptyState")?.classList.add("d-none");

        showSkeletons(6);

        try {
            const out = await fetchMatch(base, flavors, foods, moods);
            recTitle.textContent = `${out.base || "ผลลัพธ์"} • ${out.flavors?.join(", ") || "Signature"}`;

            const foodsTH = foods.map((v) => FOOD_LABEL_TH[v] || v).join(", ");
            const moodsTH = moods.map((v) => MOOD_LABEL_TH[v] || v).join(", ");
            const meta = [
                foods.length ? `ประเภทอาหาร: ${foodsTH}` : null,
                moods.length ? `บรรยากาศ: ${moodsTH}` : null,
            ].filter(Boolean).join(" • ");

            resultWrap.innerHTML = out.items.map((x) => {
                const img = x.imageUrl || "https://via.placeholder.com/640x480?text=Flavor+Match";
                const price = typeof x.price === "number" && x.price > 0 ? ` • ฿${x.price.toLocaleString()}` : "";
                const rating = x.rating ? `⭐ ${x.rating.toFixed(1)} (${x.ratingCount || 0})` : "";
                const sub = [x.type, x.province].filter(Boolean).join(" • ");
                return `
<div class="fm-result">
  <div class="reco-card">
    <img class="reco-img" src="${img}" alt="${x.name}">
    <div class="reco-body">
      <div class="d-flex justify-content-between align-items-center mb-1">
        <div class="reco-name">${x.name}</div>
        <span class="reco-badge">Score ${x.score?.toFixed?.(2) ?? "—"}</span>
      </div>
      <div class="reco-meta">${[sub, price].join("")}</div>
      ${rating ? `<div class="reco-meta mt-1">${rating}</div>` : ``}
      <div class="reco-why">${x.why || ""}</div>
      ${x.id ? `<a class="fm-btn fm-btn-ghost fm-btn-sm mt-2" href="/Detail?id=${x.id}">ดูข้อมูล</a>` : ``}
    </div>
  </div>
</div>`;
            }).join("");

            recSub.textContent = `พบ ${out.items.length} รายการ${meta ? " • " + meta : ""}`;
            resultWrap.scrollIntoView({ behavior: "smooth", block: "start" });
            saveHistory({ base, flavors, foods, moods });
            renderHistory();
        } catch (err) {
            alert(err?.message || "เกิดข้อผิดพลาด");
            clearResults();
            recTitle.textContent = "เกิดข้อผิดพลาด";
            recSub.textContent = "ลองปรับตัวเลือกใหม่ หรือรีเฟรชหน้า";
        }
    });


    // ===== Misc controls =====
    $("#btnReset")?.addEventListener("click", () => {
        $all('input[type="checkbox"]').forEach((i) => (i.checked = false));
        clearResults();
        recTitle.textContent = "ยังไม่มีคำแนะนำ";
        recSub.textContent = "ตั้งค่าทางซ้าย แล้วกด “แนะนำเลย”";
    });

    $("#btnClearHist")?.addEventListener("click", () => {
        localStorage.removeItem(HISTORY_KEY);
        renderHistory();
    });

    btnShare?.addEventListener("click", async () => {
        const base = baseEl.value;
        const flavors = $all('input[name="flavor"]:checked').map((i) => i.value);
        const foods = $all('input[name="food"]:checked').map((i) => i.value);
        const moods = $all('input[name="mood"]:checked').map((i) => i.value);
        const url = buildShareUrl({ base, flavors, foods, moods });
        try {
            await navigator.clipboard.writeText(url);
            shareToast?.classList.remove("d-none");
            setTimeout(() => shareToast?.classList.add("d-none"), 1200);
        } catch {
            alert("คัดลอกลิงก์ไม่สำเร็จ");
        }
    });

    // ===== Init =====
    renderHistory();
    loadOptionsAndRender();
})();
