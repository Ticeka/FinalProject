// ========= Helpers =========
function fmtTHB(n) { try { return new Intl.NumberFormat('th-TH').format(parseFloat(n)); } catch { return n; } }
function getEmojiByType(type) {
    const t = (type || '').toLowerCase();
    if (t.includes('ไวน์') || t.includes('wine')) return { emoji: '🍷', cls: 'poi-wine' };
    if (t.includes('เบียร์') || t.includes('beer') || t.includes('ลาเกอร์') || t.includes('ipa')) return { emoji: '🍺', cls: 'poi-beer' };
    if (t.includes('วิสกี้') || t.includes('เหล้า') || t.includes('rum') || t.includes('whisky') || t.includes('liquor')) return { emoji: '🥃', cls: 'poi-liquor' };
    return { emoji: '🍶', cls: 'poi-default' };
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
function stringToHsl(seed) { let h = 0; const str = String(seed || ''); for (let i = 0; i < str.length; i++) h = (h * 31 + str.charCodeAt(i)) % 360; return `hsl(${h} 70% 85%)`; }
function renderStars(n) { const x = Math.max(1, Math.min(5, Number(n) || 0)); return '★★★★★'.slice(0, x) + '☆☆☆☆☆'.slice(0, 5 - x); }

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
        const icon = getEmojiByType(data.type);
        L.marker([data.lat, data.lng], { icon: L.divIcon({ html: `<div class="poi-pin ${icon.cls}">${icon.emoji}</div>`, className: '', iconSize: [32, 32], iconAnchor: [16, 16], popupAnchor: [0, -14] }) })
            .addTo(map).bindPopup(data.name || 'ตำแหน่ง');
    }

    // ===== Quick Rating (ต้องล็อกอิน, เปลี่ยนคะแนนได้ตลอด, ไม่มี localStorage/อิงเครื่อง) =====
    (function quickRating() {
        const box = document.querySelector('.ratebox');
        if (!box || !data.id) return;

        const avgEl = box.querySelector('.avg');
        const cntEl = box.querySelector('.cnt');
        const btns = Array.from(box.querySelectorAll('.stars button'));
        const isAuth = truthy(root.dataset.auth);
        const loginUrl = '/Identity/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);

        let myScore = 0; // คะแนนที่ "ฉัน" ให้ (ของผู้ใช้ที่ล็อกอิน)

        function paint(score) {
            btns.forEach(b => b.classList.toggle('active', parseInt(b.dataset.score, 10) <= score));
            if (score > 0) box.title = `คุณให้ ${score} ดาวไว้แล้ว`;
            else box.title = 'คลิกเพื่อให้คะแนน';
        }

        async function fetchMine() {
            if (!isAuth) { paint(0); return; }
            try {
                const res = await fetchWithTimeout(`/api/ratings/quick/mine?beerId=${data.id}`, {}, 8000);
                if (!res.ok) { paint(0); return; }
                const js = await res.json();
                myScore = (js && js.score >= 1 && js.score <= 5) ? js.score : 0;
                paint(myScore);
            } catch { paint(0); }
        }

        async function send(score) {
            if (!isAuth) {
                if (confirm('ต้องเข้าสู่ระบบก่อนจึงจะให้คะแนนได้\nไปหน้าเข้าสู่ระบบตอนนี้ไหม?')) location.href = loginUrl;
                return;
            }
            if (box.dataset.loading === '1') return;
            box.dataset.loading = '1';
            try {
                const res = await fetchWithTimeout('/api/ratings/quick', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ beerId: data.id, score })
                });
                if (!res.ok) {
                    const t = await res.text().catch(() => '');
                    alert('ให้คะแนนไม่สำเร็จ: ' + (t || res.status)); return;
                }
                const out = await res.json(); // { avg, count }
                if (avgEl && typeof out.avg === 'number') avgEl.textContent = Number(out.avg).toFixed(1);
                if (cntEl && typeof out.count === 'number') cntEl.textContent = out.count;
                myScore = score;
                paint(myScore);
                // อัปเดตดาวในคอมเมนต์ให้เป็นคะแนนของฉันด้วย
                window.__myRatingForThisBeer = myScore;
                // repaint ปุ่มดาวในคอมเมนต์ (เฉพาะส่วนแสดง)
                document.querySelectorAll('.cmt-item .cmt-stars').forEach(el => {
                    el.innerHTML = myScore ? `${renderStars(myScore)} <span class="text-muted small">(${myScore})</span>` : '';
                });
            } catch (e) {
                console.error(e); alert('เครือข่ายมีปัญหา ลองใหม่อีกครั้ง');
            } finally { box.dataset.loading = '0'; }
        }

        // hover/leave แสดงตัวอย่างตามปุ่มที่ชี้ แต่ปล่อยเมาส์กลับมาเป็น myScore
        btns.forEach(btn => {
            btn.addEventListener('mouseenter', () => {
                const s = parseInt(btn.dataset.score, 10);
                paint(s);
            });
            btn.addEventListener('mouseleave', () => paint(myScore));
            btn.addEventListener('click', () => send(parseInt(btn.dataset.score, 10)));
            btn.addEventListener('keydown', e => {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); send(parseInt(btn.dataset.score, 10)); }
            });
        });

        // โหลดคะแนนของฉันตอนเริ่ม
        fetchMine();
        // ให้ global สำหรับส่วนคอมเมนต์ใช้
        window.__myRatingForThisBeer = myScore;
    })();

    // ===== Comments (ต้องล็อกอินตอนกดส่ง, รองรับ reply, แอดมินลบได้หมด) =====
    (function () {
        const beerEl = document.getElementById("beerData");
        if (!beerEl) return;

        const beerId = Number(beerEl.dataset.id);
        if (!Number.isInteger(beerId) || beerId <= 0) { console.error("Invalid beerId:", beerEl.dataset.id); return; }

        const loginUrl = '/Identity/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);

        const $ = s => document.querySelector(s);
        const list = $("#cmtList");
        const empty = $("#cmtEmpty");
        const txt = $("#cmtText");
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

        function itemHeader(it, nameSafe) {
            const when = new Date(it.createdAt);
            const whenStr = isNaN(when.getTime()) ? "" : when.toLocaleString("th-TH", { dateStyle: "medium", timeStyle: "short" });

            // ให้ดาวที่โชว์ตรงคอมเมนต์ = คะแนนที่ฉันให้ (ถ้ามี)
            const my = window.__myRatingForThisBeer || 0;
            const starHtml = my ? `<span class="text-warning cmt-stars" title="คะแนนของฉัน">${renderStars(my)} <span class="text-muted small">(${my})</span></span>` : `<span class="cmt-stars"></span>`;

            const delBtnHtml = it.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger cmt-del" data-id="${it.id}" aria-label="ลบความคิดเห็น">ลบ</button>` : "";
            const replyBtnHtml = `<button type="button" class="btn btn-sm btn-outline-primary cmt-reply" data-id="${it.id}">ตอบกลับ</button>`;
            return `
                <div class="d-flex justify-content-between align-items-start gap-3">
                  <div class="d-flex align-items-center gap-2">
                    ${avatarBlock(nameSafe, it.avatarUrl || null, it.profileUrl || null)}
                    <div class="d-flex align-items-center gap-2">
                      ${nameBlock(nameSafe, it.profileUrl || null)}
                      ${starHtml}
                    </div>
                  </div>
                  <div class="d-flex align-items-center gap-2">
                    <span class="text-muted small">${escapeHtml(whenStr)}</span>
                    ${replyBtnHtml}
                    ${delBtnHtml}
                  </div>
                </div>`;
        }

        function renderItem(it, depth = 0) {
            const nameRaw = it.displayName || it.author || "User";
            const nameSafe = (nameRaw?.trim() || "User");
            const el = document.createElement("div");
            el.className = "p-3 border rounded-3 cmt-item";
            el.setAttribute("data-id", it.id);
            if (depth > 0) el.style.marginLeft = Math.min(depth * 16, 48) + "px";

            el.innerHTML = `
                ${itemHeader(it, nameSafe)}
                <div class="mt-2 cmt-text">${escapeHtml(it.body || "")}</div>
                <div class="cmt-children" data-parent="${it.id}"></div>
            `;

            // ลบ
            el.querySelector(".cmt-del")?.addEventListener("click", async (e) => {
                const id = Number(e.currentTarget.getAttribute("data-id"));
                if (!Number.isInteger(id) || id <= 0) return;
                if (!confirm("ลบความคิดเห็นนี้ใช่ไหม?")) return;
                try {
                    const res = await fetchWithTimeout(`/api/beers/${beerId}/comments/${id}`, { method: "DELETE" }, 10000);
                    if (res.status === 204) await load();
                    else { const t = await res.text().catch(() => ""); alert("ลบคอมเมนต์ไม่สำเร็จ: " + (t || res.status)); }
                } catch (err) { console.error(err); alert("ลบคอมเมนต์ไม่สำเร็จ"); }
            });

            // ตอบกลับ
            el.querySelector(".cmt-reply")?.addEventListener("click", () => {
                const isAuth = truthy(root.dataset.auth);
                if (!isAuth) {
                    if (confirm("ต้องล็อกอินก่อนจึงจะตอบกลับได้\nไปหน้าเข้าสู่ระบบตอนนี้ไหม?")) location.href = loginUrl;
                    return;
                }
                const holder = el.querySelector(".cmt-children");
                if (!holder) return;

                const existing = holder.querySelector("textarea.reply-text");
                if (existing) { existing.focus(); return; }

                const form = document.createElement("div");
                form.className = "reply-form mt-2";
                form.innerHTML = `
                    <textarea class="form-control reply-text" rows="2" maxlength="1000" placeholder="พิมพ์คำตอบของคุณ…"></textarea>
                    <div class="d-flex justify-content-end gap-2 mt-2">
                        <button type="button" class="btn btn-light btn-sm reply-cancel">ยกเลิก</button>
                        <button type="button" class="btn btn-primary btn-sm reply-send">ส่งคำตอบ</button>
                    </div>
                `;
                holder.prepend(form);

                form.querySelector(".reply-cancel").addEventListener("click", () => form.remove());
                form.querySelector(".reply-send").addEventListener("click", async () => {
                    const ta = form.querySelector(".reply-text");
                    const body = (ta.value || "").trim();
                    if (!body) { ta.focus(); return; }
                    if (body.length > 1000) { alert("ข้อความยาวเกิน 1000 ตัวอักษร"); return; }

                    try {
                        const res = await fetchWithTimeout(`/api/beers/${beerId}/comments`, {
                            method: "POST",
                            headers: { "Content-Type": "application/json" },
                            body: JSON.stringify({ body, parentId: it.id })
                        }, 10000);

                        if (!res.ok) { const msg = await res.text().catch(() => ""); throw new Error(msg || `POST ${res.status}`); }
                        await load();
                    } catch (err) {
                        console.error(err);
                        alert("ส่งคำตอบไม่สำเร็จ");
                    }
                });
            });

            // วาดลูก
            if (Array.isArray(it.replies) && it.replies.length) {
                const childBox = el.querySelector(".cmt-children");
                it.replies.forEach(ch => childBox.appendChild(renderItem(ch, depth + 1)));
            }

            return el;
        }

        async function load() {
            try {
                // โหลดคอมเมนต์ + โหลดคะแนนของฉัน เพื่อเอาไปโชว์ในหัวคอมเมนต์
                const [resComments, resMine] = await Promise.all([
                    fetchWithTimeout(`/api/beers/${beerId}/comments`),
                    fetchWithTimeout(`/api/ratings/quick/mine?beerId=${beerId}`, {}, 8000).catch(() => null)
                ]);

                if (!resComments.ok) throw new Error(`GET comments ${resComments.status}`);
                const items = await resComments.json();

                // อัปเดตคะแนนของฉัน (ถ้ามี)
                let my = 0;
                if (resMine && resMine.ok) {
                    const js = await resMine.json();
                    if (js && js.score >= 1 && js.score <= 5) my = js.score;
                }
                window.__myRatingForThisBeer = my;

                list.innerHTML = "";
                if (!items.length) { if (empty) empty.style.display = ""; return; }
                if (empty) empty.style.display = "none";

                items.forEach(it => list.appendChild(renderItem(it)));
            } catch (err) {
                console.error(err);
                alert("โหลดคอมเมนต์ไม่สำเร็จ");
            }
        }

        async function sendTopLevel() {
            const isAuth = truthy(root.dataset.auth);
            if (!isAuth) {
                if (confirm("ต้องล็อกอินก่อนจึงจะคอมเมนต์ได้\nไปหน้าเข้าสู่ระบบตอนนี้ไหม?")) location.href = loginUrl;
                return;
            }
            const body = (txt?.value || "").trim();
            if (body.length === 0) { txt?.focus(); return; }
            if (body.length > 1000) { alert("ข้อความยาวเกิน 1000 ตัวอักษร"); return; }

            btnSend.disabled = true;
            btnSend.textContent = "กำลังส่ง...";

            try {
                const res = await fetchWithTimeout(`/api/beers/${beerId}/comments`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ body, parentId: null })
                }, 10000);

                if (!res.ok) { const msg = await res.text().catch(() => ""); throw new Error(msg || `POST ${res.status}`); }
                if (txt) txt.value = "";
                await load();
            } catch (err) {
                console.error(err);
                alert("ส่งคอมเมนต์ไม่สำเร็จ");
            } finally {
                btnSend.disabled = false;
                btnSend.textContent = "ส่งความคิดเห็น";
            }
        }

        btnSend?.addEventListener("click", sendTopLevel);
        btnReload?.addEventListener("click", load);

        // auto-load
        load();
    })();

    // ===== Favorite เดิม =====
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
            try { const res = await fetchWithTimeout('/api/debug/whoami', {}, 8000); if (!res.ok) return false; const x = await res.json(); isAuth = !!x.isAuth; return isAuth; }
            catch { return false; }
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
                if (confirm('ต้องล็อกอินก่อนถึงจะเพิ่ม “ถูกใจ” ได้\nไปหน้าเข้าสู่ระบบตอนนี้ไหม?')) location.href = loginUrl;
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
                    if (confirm('เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่\nไปหน้าเข้าสู่ระบบตอนนี้ไหม?')) location.href = loginUrl;
                    return;
                }
                if (!res.ok && res.status !== 204) {
                    const t = await res.text().catch(() => '');
                    alert('ดำเนินการไม่สำเร็จ: ' + (t || res.status));
                    return;
                }
                setUI(!on);
            } catch (e) { console.error(e); alert('เครือข่ายขัดข้อง ลองใหม่อีกครั้ง'); }
            finally { btn.disabled = false; }
        }

        btn.addEventListener('click', toggle);
        getStatus();
    })();
    document.addEventListener('DOMContentLoaded', () => {
        const root = document.getElementById('beerData');
        if (!root) return;
        const id = parseInt(root.dataset.id || '0', 10);
        if (!id) return;

        // กันนับซ้ำใน session ของแท็บเดียวกัน
        const key = `pv_once_${id}`;
        if (sessionStorage.getItem(key)) return;
        sessionStorage.setItem(key, '1');

        // ถ้าคุณอยากนับผ่าน API เพิ่มเติม สามารถยิงไปยัง /api/beers/{id}/view ได้
        // แต่ในวิธีหลัก เรานับแล้วใน OnGet() จึงไม่จำเป็นต้องยิง
    });
    // ยืนยันก่อนลบ (เฉพาะฟอร์มแอดมิน)
    document.addEventListener('DOMContentLoaded', () => {
        const delForm = document.getElementById('deleteBeerForm');
        if (delForm) {
            delForm.addEventListener('submit', (e) => {
                if (!confirm('ยืนยันลบข้อมูลนี้ถาวร?\n(ไม่สามารถกู้คืนได้)')) {
                    e.preventDefault();
                }
            });
        }
    });

});
