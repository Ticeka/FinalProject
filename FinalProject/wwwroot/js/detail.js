// ========= Helpers =========
function fmtTHB(n) {
    try { return new Intl.NumberFormat('th-TH').format(parseFloat(n)); } catch { return n; }
}
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
        phone: root.dataset.phone || ''
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

    // 4) ให้ดาวแบบไม่ล็อกอิน
    (function quickRating() {
        const box = document.querySelector('.ratebox');
        if (!box || !data.id) return;

        const avgEl = box.querySelector('.avg');
        const cntEl = box.querySelector('.cnt');
        const btns = Array.from(box.querySelectorAll('.stars button'));
        const key = `qrated_${data.id}`;

        // ถ้าโหวตแล้ว ไฮไลท์ไว้
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
            if (box.dataset.loading === '1') return; // กันกดรัว
            box.dataset.loading = '1';

            try {
                const res = await fetch('/api/ratings/quick', {
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
            }
            catch (e) {
                console.error(e);
                alert('เครือข่ายมีปัญหา ลองใหม่อีกครั้ง');
            }
            finally {
                box.dataset.loading = '0';
            }
        }

        // hover preview + click ส่งคะแนน
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
});
