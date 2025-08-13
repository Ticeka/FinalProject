// ========= Helpers =========
function fmtTHB(n) { try { return new Intl.NumberFormat('th-TH').format(parseFloat(n)); } catch { return n; } }
function getEmojiByType(type) {
    const t = (type || '').toLowerCase();
    if (t.includes('ไวน์') || t.includes('wine')) return { emoji: '🍷', cls: 'poi-wine' };
    if (t.includes('เบียร์') || t.includes('beer') || t.includes('ลาเกอร์') || t.includes('ipa')) return { emoji: '🍺', cls: 'poi-beer' };
    if (t.includes('วิสกี้') || t.includes('เหล้า') || t.includes('rum') || t.includes('whisky') || t.includes('liquor')) return { emoji: '🥃', cls: 'poi-liquor' };
    return { emoji: '🍶', cls: 'poi-default' };
}
function createEmojiIcon(type) {
    const { emoji, cls } = getEmojiByType(type);
    return L.divIcon({ html: `<div class="poi-pin ${cls}">${emoji}</div>`, className: '', iconSize: [32, 32], iconAnchor: [16, 16], popupAnchor: [0, -14] });
}
async function fetchWithTimeout(url, opts = {}, ms = 10000) {
    const ctrl = new AbortController();
    const id = setTimeout(() => ctrl.abort(new DOMException("timeout", "AbortError")), ms);
    const headers = Object.assign({ 'Accept': 'application/json' }, opts.headers || {});
    try { return await fetch(url, { credentials: 'include', ...opts, headers, signal: ctrl.signal }); }
    finally { clearTimeout(id); }
}
function truthy(s) { const v = (s ?? '').toString().trim().toLowerCase(); return v === 'true' || v === '1' || v === 'yes'; }
function escapeHtml(s) { const d = document.createElement('div'); d.innerText = s ?? ""; return d.innerHTML; }
function escapeAttr(s) { return String(s ?? '').replace(/"/g, '&quot;'); }
function getInitials(name) { const t = (name || '').trim(); if (!t) return 'U'; const parts = t.split(/\s+/).slice(0, 2); return parts.map(p => p[0]).join('').toUpperCase(); }
function stringToHsl(seed) {
    let h = 0; const str = String(seed || '');
    for (let i = 0; i < str.length; i++) h = (h * 31 + str.charCodeAt(i)) % 360;
    return `hsl(${h} 70% 85%)`;
}
function renderStars(n) {
    const x = Math.max(1, Math.min(5, Number(n) || 0));
    return '★★★★★'.slice(0, x) + '☆☆☆☆☆'.slice(0, 5 - x);
}

// ========= Init =========
document.addEventListener('DOMContentLoaded', () => {
    const root = document.getElementById('beerData');
    if (!root) return;

    const data = {
        id: parseInt(root.dataset.id || '0', 10),
        name: root.dataset.name || '',
        type: root.dataset.type || '',
        province: root.dataset.province || '',
        description: root.dataset.description || '',
        lat: parseFloat(root.dataset.lat || '0'),
        lng: parseFloat(root.dataset.lng || '0'),
        price: parseFloat(root.dataset.price || '0'),
        abv: parseFloat(root.dataset.abv || '0'),
        rating: parseFloat(root.dataset.rating || '0'),
        count: parseInt(root.dataset.count || '0', 10),
        website: root.dataset.website || '',
        facebook: root.dataset.facebook || '',
        phone: root.dataset.phone || '',
        isAuthHint: truthy(root.dataset.auth)
    };

    // 1) ฟอร์แมตราคา
    const p1 = document.getElementById('priceVal'); if (p1) p1.textContent = fmtTHB(p1.textContent);
    const p2 = document.getElementById('priceVal2'); if (p2) p2.textContent = fmtTHB(p2.textContent);

    // 2) คัดลอกลิงก์
    document.getElementById('copyLinkBtn')?.addEventListener('click', async () => {
        try { await navigator.clipboard.writeText(location.href); alert('คัดลอกลิงก์แล้ว!'); }
        catch { alert('คัดลอกไม่สำเร็จ'); }
    });

    // 3) แผนที่
    if (!isNaN(data.lat) && !isNaN(data.lng) && data.lat !== 0 && data.lng !== 0) {
        const gbtn = document.getElementById('gmapsBtn');
        if (gbtn) gbtn.href = `https://www.google.com/maps?q=${data.lat},${data.lng}`;

        const map = L.map('map').setView([data.lat, data.lng], 13);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap contributors' }).addTo(map);
        const icon = createEmojiIcon(data.type);
        L.marker([data.lat, data.lng], { icon }).addTo(map).bindPopup(data.name || 'ตำแหน่ง');
    }

    // 4) ให้ดาวแบบไม่ล็อกอิน (Quick Rating)
    (function quickRating() {
        const box = document.querySelector('.ratebox');
        if (!box || !data.id) return;

        const avgEl = box.querySelector('.avg');
        const cntEl = box.querySelector('.cnt');
        const btns = Array.from(box.querySelectorAll('.stars button'));
        const key = `qrated_${data.id}`;

        const voted = parseInt(localStorage.getItem(key) || '0', 10);
        if (voted) {
            btns.forEach(b => b.classList.toggle('active', parseInt(b.dataset.score, 10) <= voted));
            box.title = `คุณให้ ${voted} ดาวไว้แล้ว`;
        }

        function getFingerprint() {
            let fp = localStorage.getItem('fp_uid');
            if (!fp) {
                fp = crypto.getRandomValues(new Uint32Array(4)).join('-');
                localStorage.setItem('fp_uid', fp);
            }
            return fp;
        }

        async function send(score) {
            if (box.dataset.loading === '1') return;
            box.dataset.loading = '1';
            try {
                const res = await fetchWithTimeout('/api/ratings/quick', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ beerId: data.id, score, fingerprint: getFingerprint() })
                });
                if (!res.ok) {
                    const t = await res.text().catch(() => '');
                    alert('ให้คะแนนไม่สำเร็จ: ' + (t || res.status));
                    return;
                }
                const out = await res.json(); // { avg, count }
                if (avgEl) avgEl.textContent = Number(out.avg).toFixed(1);
                if (cntEl) cntEl.textContent = out.count;
                btns.forEach(b => b.classList.toggle('active', parseInt(b.dataset.score, 10) <= score));
                localStorage.setItem(key, String(score));
                box.title = `ขอบคุณสำหรับคะแนน ${score} ดาว!`;
            } catch (e) {
                console.error(e);
                alert('เครือข่ายมีปัญหา ลองใหม่อีกครั้ง');
            } finally {
                box.dataset.loading = '0';
            }
        }

        btns.forEach(btn => {
            btn.addEventListener('mouseenter', () => {
                const s = parseInt(btn.dataset.score, 10);
                btns.forEach(b => b.classList.toggle('active', parseInt(b.dataset.score, 10) <= s));
            });
            btn.addEventListener('mouseleave', () => {
                const s = parseInt(localStorage.getItem(key) || '0', 10);
                btns.forEach(b => b.classList.toggle('active', s && parseInt(b.dataset.score, 10) <= s));
            });
            btn.addEventListener('click', () => send(parseInt(btn.dataset.score, 10)));
            btn.addEventListener('keydown', e => {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); send(parseInt(btn.dataset.score, 10)); }
            });
        });
    })();

    // ===== Comments =====
    (function () {
        const beerEl = document.getElementById("beerData");
        if (!beerEl) return;

        const beerId = Number(beerEl.dataset.id);
        if (!Number.isInteger(beerId) || beerId <= 0) {
            console.error("Invalid beerId:", beerEl.dataset.id);
            return;
        }

        const $ = s => document.querySelector(s);
        const list = $("#cmtList");
        const empty = $("#cmtEmpty");
        const txt = $("#cmtText");
        const name = $("#cmtName");
        const btnSend = $("#cmtSendBtn");
        const btnReload = $("#cmtReloadBtn");

        function avatarBlock(nameSafe, avatarUrl, profileUrl) {
            const initials = getInitials(nameSafe);
            if (avatarUrl) {
                const img = `<img src="${escapeAttr(avatarUrl)}" alt="${escapeAttr(nameSafe)}" width="36" height="36" class="rounded-circle border">`;
                return profileUrl ? `<a href="${escapeAttr(profileUrl)}" class="d-inline-block">${img}</a>` : img;
            }
            const pill = `<div class="d-inline-flex align-items-center justify-content-center rounded-circle border" style="width:36px;height:36px;background:${stringToHsl(nameSafe)};font-weight:600">${escapeHtml(initials)}</div>`;
            return profileUrl ? `<a href="${escapeAttr(profileUrl)}" class="d-inline-block">${pill}</a>` : pill;
        }

        function nameBlock(nameSafe, profileUrl) {
            return profileUrl
                ? `<a href="${escapeAttr(profileUrl)}" class="cmt-name text-decoration-none">${escapeHtml(nameSafe)}</a>`
                : `<strong>${escapeHtml(nameSafe)}</strong>`;
        }

        function renderItem(it) {
            const el = document.createElement("div");
            el.className = "p-3 border rounded-3";

            const nameRaw = it.displayName || it.author || (it.email ? String(it.email).split("@")[0] : "") || "Guest";
            const nameSafe = nameRaw.trim() || "Guest";
            const avatarUrl = (it.avatarUrl || it.avatar || it.photoUrl || "").trim() || null;
            const profileUrl = (it.profileUrl || "").trim() || null;

            const rRaw = Number(it.rating ?? it.userRating ?? it.stars);
            const starNum = Number.isFinite(rRaw) ? Math.max(1, Math.min(5, Math.round(rRaw))) : 0;

            const when = new Date(it.createdAt);
            const whenStr = isNaN(when.getTime())
                ? ""
                : when.toLocaleString("th-TH", { dateStyle: "medium", timeStyle: "short" });

            const delBtnHtml = it.canDelete
                ? `<button type="button" class="btn btn-sm btn-outline-danger cmt-del" data-id="${it.id}" aria-label="ลบความคิดเห็น">ลบ</button>`
                : "";

            el.innerHTML = `
        <div class="d-flex justify-content-between align-items-start gap-3">
          <div class="d-flex align-items-center gap-2">
            ${avatarBlock(nameSafe, avatarUrl, profileUrl)}
            <div class="d-flex align-items-center gap-2">
              ${nameBlock(nameSafe, profileUrl)}
              ${starNum ? `<span class="text-warning" title="ให้คะแนน ${starNum} ดาว">${renderStars(starNum)} <span class="text-muted small">(${starNum})</span></span>` : ``}
            </div>
          </div>
          <div class="d-flex align-items-center gap-2">
            <span class="text-muted small">${escapeHtml(whenStr)}</span>
            ${delBtnHtml}
          </div>
        </div>
        <div class="mt-2" style="white-space:pre-wrap">${escapeHtml(it.body || "")}</div>
      `;

            if (it.canDelete) {
                el.querySelector(".cmt-del")?.addEventListener("click", (e) => {
                    const id = Number(e.currentTarget.getAttribute("data-id"));
                    if (Number.isInteger(id) && id > 0) removeComment(id);
                });
            }
            return el;
        }

        async function removeComment(commentId) {
            if (!confirm("ลบความคิดเห็นนี้ใช่ไหม?")) return;
            try {
                const res = await fetchWithTimeout(`/api/beers/${beerId}/comments/${commentId}`, { method: "DELETE" }, 10000);
                if (res.status === 204) { await load(); return; }
                const t = await res.text().catch(() => "");
                alert("ลบคอมเมนต์ไม่สำเร็จ: " + (t || res.status));
            } catch (err) {
                console.error(err);
                const isAbort = err?.name === "AbortError" || err === "timeout";
                alert(isAbort ? "เครือข่ายช้า/ขาดการเชื่อมต่อ" : "ลบคอมเมนต์ไม่สำเร็จ");
            }
        }

        async function load() {
            try {
                const res = await fetchWithTimeout(`/api/beers/${beerId}/comments?skip=0&take=20`);
                if (!res.ok) throw new Error(`GET ${res.status}`);
                const items = await res.json();

                list.innerHTML = "";
                if (!items.length) { empty.style.display = ""; return; }
                empty.style.display = "none";

                items.forEach(it => list.appendChild(renderItem(it)));
            } catch (err) {
                console.error(err);
                alert("โหลดคอมเมนต์ไม่สำเร็จ");
            }
        }

        async function send(e) {
            e?.preventDefault?.();
            const body = (txt?.value || "").trim();
            if (body.length === 0) { txt?.focus(); return; }
            if (body.length > 1000) { alert("ข้อความยาวเกิน 1000 ตัวอักษร"); return; }

            btnSend.disabled = true;
            btnSend.textContent = "กำลังส่ง...";

            try {
                const key = `qrated_${beerId}`;
                const userRating = parseInt(localStorage.getItem(key) || '0', 10) || null;
                const payload = { body, displayName: name ? name.value : null, userRating };
                const res = await fetchWithTimeout(`/api/beers/${beerId}/comments`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                }, 10000);

                if (!res.ok) {
                    const msg = await res.text().catch(() => "");
                    throw new Error(msg || `POST ${res.status}`);
                }

                txt.value = ""; if (name) name.value = "";
                await load();
            } catch (err) {
                console.error(err);
                const isAbort = err?.name === "AbortError" || err === "timeout";
                alert(isAbort ? "เครือข่ายช้า/ขาดการเชื่อมต่อ" : "ส่งคอมเมนต์ไม่สำเร็จ");
            } finally {
                btnSend.disabled = false;
                btnSend.textContent = "ส่งความคิดเห็น";
            }
        }

        btnSend?.addEventListener("click", send);
        btnReload?.addEventListener("click", load);

        // auto-load
        load();
    })();

    // ===== Favorite (ต้องล็อกอิน) =====
    (function favoriteFeature() {
        const root = document.getElementById('beerData');
        if (!root) return;
        const beerId = parseInt(root.dataset.id || '0', 10);
        const btn = document.getElementById('favBtn');
        if (!btn || !beerId) return;

        let isAuth = truthy(root.dataset.auth);
        const loginUrl = '/Identity/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);

        function setUI(on) {
            btn.classList.toggle('fav-on', !!on);
            btn.setAttribute('aria-pressed', on ? 'true' : 'false');
            btn.textContent = (on ? 'ถูกใจแล้ว' : 'ถูกใจ');
        }

        async function ensureAuthState() {
            if (isAuth) return true;
            try {
                const res = await fetchWithTimeout('/api/debug/whoami', {}, 8000);
                if (!res.ok) return false;
                const x = await res.json();
                isAuth = !!x.isAuth;
                return isAuth;
            } catch { return false; }
        }

        async function getStatus() {
            const okAuth = await ensureAuthState();
            if (!okAuth) { setUI(false); return; }
            try {
                const res = await fetchWithTimeout(`/api/beers/${beerId}/favorite`);
                if (res.status === 401) { setUI(false); return; }
                if (!res.ok) throw new Error();
                const js = await res.json();
                setUI(!!js.isFavorite);
            } catch { }
        }

        async function toggle() {
            const okAuth = await ensureAuthState();
            if (!okAuth) {
                if (confirm('ต้องล็อกอินก่อนถึงจะเพิ่ม “ถูกใจ” ได้\nไปหน้าเข้าสู่ระบบตอนนี้ไหม?')) {
                    location.href = loginUrl;
                }
                return;
            }

            const on = btn.classList.contains('fav-on');
            btn.disabled = true;
            try {
                const res = await fetchWithTimeout(`/api/beers/${beerId}/favorite`, {
                    method: on ? 'DELETE' : 'POST',
                    headers: { 'Content-Type': 'application/json' }
                });

                if (res.status === 401) {
                    if (confirm('เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่\nไปหน้าเข้าสู่ระบบตอนนี้ไหม?')) {
                        location.href = loginUrl;
                    }
                    return;
                }

                if (!res.ok && res.status !== 204) {
                    const t = await res.text().catch(() => '');
                    alert('ดำเนินการไม่สำเร็จ: ' + (t || res.status));
                    return;
                }

                setUI(!on);
            } catch (e) {
                console.error(e);
                alert('เครือข่ายขัดข้อง ลองใหม่อีกครั้ง');
            } finally {
                btn.disabled = false;
            }
        }

        btn.addEventListener('click', toggle);
        getStatus();
    })();
});
