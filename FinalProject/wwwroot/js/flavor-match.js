(function () {
    const $ = (s, p = document) => p.querySelector(s);
    const $all = (s, p = document) => [...p.querySelectorAll(s)];
    const form = $("#matchForm");
    const baseEl = $("#base");
    const btnReset = $("#btnReset");
    const btnShare = $("#btnShare");
    const shareToast = $("#shareToast");
    const resultWrap = $("#resultWrap");
    const recTitle = $("#recTitle");
    const recSub = $("#recSub");
    const histList = $("#histList");
    const btnClearHist = $("#btnClearHist");

    const API = "/api/reco/flavor-match";
    const HISTORY_KEY = "flavor_match_hist";

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
        <div class="s">${it.base} • ${it.flavors.join(", ") || "—"}</div>
      </div>
    `).join("");
    }
    function buildShareUrl(base, flavors) {
        const u = new URL(location.href);
        u.searchParams.set("base", base);
        u.searchParams.set("flavors", flavors.join(","));
        return u.toString();
    }
    function cardHtml(x) {
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
            <div class="reco-meta">${meta}${price ? price : ""}</div>
            ${rating ? `<div class="reco-meta mt-1">${rating}</div>` : ``}
            <div class="reco-why">${x.why || ""}</div>
            ${x.id ? `<a class="btn btn-sm btn-soft mt-2" href="/Detail?id=${x.id}">ดูข้อมูล</a>` : ``}
          </div>
        </div>
      </div>`;
    }

    async function fetchReco(base, flavors) {
        const payload = { base, flavors, take: 6 };
        const res = await fetch(API, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload),
            credentials: "same-origin"
        });
        if (!res.ok) throw new Error(await res.text());
        return await res.json(); // { base, flavors, items:[] }
    }

    form?.addEventListener("submit", async (e) => {
        e.preventDefault();
        const base = baseEl.value;
        const flavors = $all('input[name="flavor"]:checked', form).map(i => i.value);

        if (!base) { alert("กรุณาเลือกฐานเครื่องดื่ม"); return; }

        recTitle.textContent = "กำลังผสมรสที่ใช่...";
        recSub.textContent = "กำลังค้นหาที่เข้ากับรสชาติและคะแนนรีวิว";
        resultWrap.classList.add("d-none");

        try {
            const out = await fetchReco(base, flavors);
            recTitle.textContent = `${out.base} • ${out.flavors.join(", ") || "Signature"}`;
            recSub.textContent = `พบคำแนะนำ ${out.items.length} รายการ`;
            resultWrap.innerHTML = out.items.map(cardHtml).join("");
            resultWrap.classList.remove("d-none");

            // history
            saveHistory({ title: recTitle.textContent, base: out.base, flavors: out.flavors });
            renderHistory();
            history.replaceState(null, "", buildShareUrl(base, flavors));

        } catch (err) {
            console.error(err);
            recTitle.textContent = "ขออภัย แนะนำไม่สำเร็จ";
            recSub.textContent = "ลองใหม่อีกครั้ง หรือตรวจสอบการเชื่อมต่อ";
            resultWrap.classList.add("d-none");
        }
    });

    btnReset?.addEventListener("click", () => {
        form.reset();
        resultWrap.classList.add("d-none");
        recTitle.textContent = "—";
        recSub.textContent = "เลือกฐานและรสที่ชอบ แล้วกด “แนะนำเลย”";
        history.replaceState(null, "", location.pathname);
    });

    btnShare?.addEventListener("click", async () => {
        const base = baseEl.value || "";
        const flavors = $all('input[name="flavor"]:checked', form).map(i => i.value);
        const url = buildShareUrl(base, flavors);
        try {
            await navigator.clipboard.writeText(url);
            shareToast?.classList.remove("d-none");
            setTimeout(() => shareToast?.classList.add("d-none"), 1500);
        } catch { alert("คัดลอกลิงก์ไม่สำเร็จ"); }
    });

    btnClearHist?.addEventListener("click", () => {
        localStorage.removeItem(HISTORY_KEY);
        renderHistory();
    });

    // Deep link restore
    const sp = new URLSearchParams(location.search);
    if (sp.has("base")) baseEl.value = sp.get("base");
    if (sp.has("flavors")) {
        sp.get("flavors").split(",").map(s => s.trim()).filter(Boolean).forEach(v => {
            const el = $(`input[name="flavor"][value="${CSS.escape(v)}"]`, form);
            if (el) el.checked = true;
        });
    }
    renderHistory();
})();
