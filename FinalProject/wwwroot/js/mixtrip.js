(function () {
    const $ = (s, p = document) => p.querySelector(s);
    const $all = (s, p = document) => [...p.querySelectorAll(s)];

    // ---------- data ----------
    const DESTS = [
        { name: "ภูเก็ต", mood: "Chill by Sea", map: "7.8804,98.3923", why: "หาดสวย บีชบาร์เยอะ บรรยากาศปลายวัน" },
        { name: "เชียงใหม่", mood: "Mountain Escape", map: "18.7883,98.9853", why: "อากาศดี คาเฟ่วิวเขา พร้อมบาร์คราฟต์" },
        { name: "อยุธยา", mood: "Old Town Walk", map: "14.3532,100.5689", why: "เดินเมืองเก่า ชิมกับแกล้มท้องถิ่น" },
        { name: "ขอนแก่น", mood: "Night Market Hop", map: "16.4419,102.8350", why: "ไนท์มาร์เก็ตคึกคัก ของกินเพียบ" },
        { name: "ตรัง", mood: "Island Hopping", map: "7.5594,99.6111", why: "เกาะเงียบ น้ำใส อาหารทะเลเด็ด" },
        { name: "นครปฐม", mood: "Café Crawl", map: "13.8199,100.0621", why: "คาเฟ่สวย แนวธรรมชาติ ใกล้กรุง" },
    ];
    const TRY = [
        { base: "Beer", what: "คราฟต์เบียร์ท้องถิ่น", note: "มอลต์-ฮอปบาลานซ์ ลองสไตล์ Lager/IPA" },
        { base: "Wine", what: "ไวน์ผลไม้ท้องถิ่น", note: "จับคู่ชีส/ผลไม้รสเปรี้ยว" },
        { base: "Whisky", what: "วิสกี้สโมคกี้", note: "จิบเพียวกับน้ำแข็งก้อนใส" },
        { base: "Rum", what: "รัมอ้อยบ้าน", note: "ลอง highball กับโทนิค" },
        { base: "Gin", what: "จินโทนิกสมุนไพร", note: "แตงกวา/โรสแมรี่เพิ่มกลิ่น" },
        { base: "Thai Craft", what: "สุราท้องถิ่น", note: "จิบช้า ๆ กับน้ำโซดาเย็น" },
        { base: "Mocktail", what: "ม็อคเทลผลไม้", note: "เน้น Citrus/Fruity สดชื่น" },
    ];

    // ---------- helpers ----------
    const seedFrom = (str) => {
        let h = 2166136261 >>> 0;
        for (let i = 0; i < str.length; i++) { h ^= str.charCodeAt(i); h += (h << 1) + (h << 4) + (h << 7) + (h << 8) + (h << 24); }
        return h >>> 0;
    };
    const rng = (seed) => () => (seed = (seed * 1664525 + 1013904223) >>> 0) / 4294967296;

    const pick = (arr, r) => arr[Math.floor(r() * arr.length)];

    const titleFrom = (base, flavors) => {
        const f = (flavors[0] || "Signature");
        return `${f} ${base} Twist`;
    };

    const buildShareUrl = (base, flavors, mood) => {
        const u = new URL(location.href);
        u.searchParams.set("base", base);
        u.searchParams.set("mood", mood);
        u.searchParams.set("flavor", flavors.join(","));
        return u.toString();
    };

    const saveHistory = (obj) => {
        const key = "mixtrip_hist";
        const arr = JSON.parse(localStorage.getItem(key) || "[]");
        arr.unshift({ ...obj, t: Date.now() });
        while (arr.length > 8) arr.pop();
        localStorage.setItem(key, JSON.stringify(arr));
    };

    const renderHistory = () => {
        const box = $("#histList");
        if (!box) return;
        const arr = JSON.parse(localStorage.getItem("mixtrip_hist") || "[]");
        box.innerHTML = arr.map(it => `
      <div class="hist-item">
        <div class="t">${it.title}</div>
        <div class="s">${it.where} • ${it.base}</div>
      </div>
    `).join("");
    };

    // ---------- DOM ----------
    const form = $("#mixForm");
    const resultWrap = $("#resultWrap");
    const baseEl = $("#base");
    const moodEl = $("#mood");
    const btnReset = $("#btnReset");
    const btnAgain = $("#btnAgain");
    const btnShare = $("#btnShare");
    const shareToast = $("#shareToast");

    const mixTitle = $("#mixTitle");
    const mixBadge = $("#mixBadge");
    const mixDesc = $("#mixDesc");
    const tripWhere = $("#tripWhere");
    const tripWhy = $("#tripWhy");
    const tripMap = $("#tripMap");
    const whatTry = $("#whatTry");
    const whatNote = $("#whatNote");

    const runMix = (base, flavors, mood) => {
        const seed = seedFrom(`${base}|${flavors.join("-")}|${mood}`);
        const rand = rng(seed);

        const dests = DESTS.filter(d => d.mood === mood);
        const dest = dests.length ? pick(dests, rand) : pick(DESTS, rand);
        const recs = TRY.filter(t => t.base === base);
        const rec = recs.length ? pick(recs, rand) : pick(TRY, rand);

        // fill UI
        mixTitle.textContent = titleFrom(base, flavors);
        mixBadge.textContent = mood;
        mixDesc.textContent = `ฐาน ${base} • โทน: ${flavors.join(", ") || "—"}`;
        tripWhere.textContent = dest.name;
        tripWhy.textContent = dest.why;
        tripMap.href = `https://www.google.com/maps?q=${dest.map}`;
        whatTry.textContent = rec.what;
        whatNote.textContent = rec.note;

        resultWrap.classList.remove("d-none");

        // history
        saveHistory({ title: mixTitle.textContent, where: dest.name, base, mood });
        renderHistory();
    };

    form?.addEventListener("submit", (e) => {
        e.preventDefault();
        const base = baseEl.value;
        const mood = moodEl.value;
        const flavors = $all('input[name="flavor"]:checked', form).map(i => i.value);
        if (!base || !mood) { alert("กรุณาเลือก ฐานเครื่องดื่ม และ อารมณ์ทริป"); return; }
        runMix(base, flavors, mood);
        history.replaceState(null, "", buildShareUrl(base, flavors, mood));
    });

    btnReset?.addEventListener("click", () => {
        form.reset();
        resultWrap.classList.add("d-none");
        shareToast?.classList.add("d-none");
        history.replaceState(null, "", location.pathname);
    });

    btnAgain?.addEventListener("click", () => {
        // กดสุ่มใหม่ด้วยค่าที่เลือกเดิม
        const base = baseEl.value;
        const mood = moodEl.value;
        const flavors = $all('input[name="flavor"]:checked', form).map(i => i.value);
        if (!base || !mood) return;
        // เพิ่มความสุ่มด้วย flavor order สลับเล็กน้อย
        flavors.sort(() => Math.random() - 0.5);
        runMix(base, flavors, mood);
    });

    btnShare?.addEventListener("click", async () => {
        const base = baseEl.value;
        const mood = moodEl.value;
        const flavors = $all('input[name="flavor"]:checked', form).map(i => i.value);
        const url = buildShareUrl(base, flavors, mood);
        try {
            await navigator.clipboard.writeText(url);
            shareToast?.classList.remove("d-none");
            setTimeout(() => shareToast?.classList.add("d-none"), 1500);
        } catch { alert("คัดลอกลิงก์ไม่สำเร็จ"); }
    });

    // deep-link restore
    const sp = new URLSearchParams(location.search);
    if (sp.has("base")) baseEl.value = sp.get("base");
    if (sp.has("mood")) moodEl.value = sp.get("mood");
    if (sp.has("flavor")) {
        const fs = sp.get("flavor").split(",").map(s => s.trim()).filter(Boolean);
        fs.forEach(v => {
            const el = $(`input[name="flavor"][value="${CSS.escape(v)}"]`);
            if (el) el.checked = true;
        });
    }
    if (sp.has("base") && sp.has("mood")) {
        // auto-run for shared links
        const flavors = $all('input[name="flavor"]:checked', form).map(i => i.value);
        runMix(baseEl.value, flavors, moodEl.value);
    }

    renderHistory();
})();
