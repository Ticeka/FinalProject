// ===== Helpers =====
const $ = (sel, root = document) => root.querySelector(sel);
const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));

function showToast(message) {
    const toastEl = $('#profileToast');
    if (!toastEl) return;
    $('#toastMsg').textContent = message || 'ดำเนินการสำเร็จ';
    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 2200 });
    toast.show();
}

function setUploading(visible, percent = 0) {
    const wrap = $('#uploadProgressWrap');
    const bar = $('#uploadProgress');
    if (!wrap || !bar) return;
    wrap.classList.toggle('d-none', !visible);
    wrap.setAttribute('aria-hidden', visible ? 'false' : 'true');
    bar.style.width = `${percent}%`;
}

function fmtTHB(n) {
    try { return new Intl.NumberFormat('th-TH').format(+n); } catch { return n; }
}

// ===== Sticky header micro interaction =====
(function headerScroll() {
    let lastY = 0;
    const hdr = $('.profile-header');
    if (!hdr) return;
    window.addEventListener('scroll', () => {
        const y = window.scrollY || 0;
        const shrink = y > 120;
        hdr.style.transform = shrink ? 'translateY(-6px) scale(.99)' : '';
        hdr.style.opacity = shrink ? '.98' : '1';
        lastY = y;
    }, { passive: true });
})();

// ===== Tabs =====
(function tabs() {
    const bar = $('.tabs');
    const underline = $('.tabs .tab-underline');
    const btns = $$('.tabs .tab-btn');
    const panels = {
        favorites: $('#tab-favorites'),
        activity: $('#tab-activity'),
        about: $('#tab-about'),
    };

    function activate(name) {
        btns.forEach(b => b.classList.toggle('active', b.dataset.tab === name));
        Object.entries(panels).forEach(([k, el]) => el?.classList.toggle('active', k === name));
        // underline move
        const activeBtn = btns.find(b => b.classList.contains('active'));
        if (activeBtn && underline) {
            const r = activeBtn.getBoundingClientRect();
            const br = bar.getBoundingClientRect();
            underline.style.transform = `translateX(${r.left - br.left}px)`;
            underline.style.width = `${r.width}px`;
        }
        // lazy load data
        if (name === 'favorites') loadFavorites();
        if (name === 'activity') attachActivityOnce();
    }

    btns.forEach(b => b.addEventListener('click', () => activate(b.dataset.tab)));
    window.addEventListener('resize', () => {
        const act = btns.find(b => b.classList.contains('active'));
        if (act) activate(act.dataset.tab);
    });

    // init
    setTimeout(() => activate('favorites'), 0);
})();

// ===== Avatar Upload / Remove =====
(function initAvatar() {
    const changeBtn = $('#changeAvatarBtn');
    const removeBtn = $('#removeAvatarBtn');
    const fileInput = $('#avatarUpload');
    const avatarWrap = document.querySelector('.avatar-wrap');

    if (!changeBtn || !fileInput || !avatarWrap) return;

    const EP = window.ProfileEndpoints || {};
    const uploadUrl = EP.upload || '/Profile/Index?handler=UploadAvatar';
    const removeUrl = EP.remove || '/Profile/Index?handler=RemoveAvatar';

    // Click to open picker
    changeBtn.addEventListener('click', () => fileInput.click());

    // Drag & drop
    ['dragover', 'dragenter'].forEach(evt =>
        avatarWrap.addEventListener(evt, e => {
            e.preventDefault(); e.stopPropagation();
            avatarWrap.classList.add('dragover');
        })
    );
    ['dragleave', 'drop'].forEach(evt =>
        avatarWrap.addEventListener(evt, e => {
            e.preventDefault(); e.stopPropagation();
            avatarWrap.classList.remove('dragover');
        })
    );
    avatarWrap.addEventListener('drop', e => {
        const file = e.dataTransfer.files?.[0];
        if (file) handleAvatarFile(file);
    });

    fileInput.addEventListener('change', () => {
        const file = fileInput.files?.[0];
        if (file) handleAvatarFile(file);
    });

    function handleAvatarFile(file) {
        if (!/^image\/(jpeg|png|webp|gif|bmp)$/i.test(file.type)) { showToast('ไฟล์ภาพไม่ถูกต้อง'); return; }
        if (file.size > 3 * 1024 * 1024) { showToast('ไฟล์ใหญ่เกิน 3 MB'); return; }

        // Preview
        const reader = new FileReader();
        reader.onload = () => {
            let img = $('#avatarPreview');
            let fallback = $('#avatarFallback');
            if (!img) {
                img = document.createElement('img');
                img.id = 'avatarPreview';
                img.alt = 'Avatar';
                img.className = 'avatar-img';
                if (fallback) fallback.replaceWith(img);
                else document.querySelector('.avatar-wrap')?.appendChild(img);
            }
            img.src = reader.result;
        };
        reader.readAsDataURL(file);

        // Upload with fetch (no progress by default, simulate steps)
        const fd = new FormData();
        fd.append('file', file);
        setUploading(true, 20);

        fetch(uploadUrl, { method: 'POST', body: fd })
            .then(async res => {
                if (!res.ok) throw new Error(await res.text());
                return res.json().catch(() => ({}));
            })
            .then(data => {
                setUploading(true, 100);
                setTimeout(() => setUploading(false, 0), 600);
                showToast('อัปโหลดรูปโปรไฟล์เรียบร้อย');
                if (data?.avatarUrl) {
                    const img = $('#avatarPreview');
                    if (img) {
                        const bust = data.avatarUrl.includes('?') ? '&' : '?';
                        img.src = data.avatarUrl + bust + 'v=' + Date.now(); // กันแคช
                    }
                }
                removeBtn?.removeAttribute('disabled');
            })
            .catch(err => {
                setUploading(false, 0);
                showToast('อัปโหลดไม่สำเร็จ');
                console.error(err);
            });
    }

    if (removeBtn) {
        removeBtn.addEventListener('click', () => {
            if (removeBtn.classList.contains('disabled')) return;
            if (!confirm('ลบรูปโปรไฟล์หรือไม่?')) return;

            fetch(removeUrl, { method: 'POST' })
                .then(res => { if (!res.ok) throw new Error('remove failed'); return res.json().catch(() => ({})); })
                .then(() => {
                    showToast('ลบรูปโปรไฟล์แล้ว');
                    const img = $('#avatarPreview');
                    if (img) {
                        const fallback = document.createElement('div');
                        fallback.id = 'avatarFallback';
                        fallback.className = 'avatar-fallback';
                        const span = document.createElement('span');
                        span.textContent = (img.getAttribute('alt-initials')?.trim()) || ($('#avatarFallback')?.dataset.initials) || '?';
                        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
                        svg.setAttribute('class', 'avatar-icon'); svg.setAttribute('viewBox', '0 0 24 24');
                        svg.innerHTML = '<circle cx="12" cy="8" r="4"></circle><path d="M4 20a8 8 0 0 1 16 0"></path>';
                        fallback.append(span, svg);
                        img.replaceWith(fallback);
                    }
                    removeBtn.classList.add('disabled');
                })
                .catch(err => {
                    showToast('ลบรูปไม่สำเร็จ');
                    console.error(err);
                });
        });
    }
})();

// ===== Edit Profile (Modal) =====
(function initEditProfile() {
    const editBtn = $('#editProfileBtn');
    const modalEl = $('#editProfileModal');
    if (!editBtn || !modalEl) return;

    const modal = new bootstrap.Modal(modalEl);
    const EP = window.ProfileEndpoints || {};
    const editUrl = EP.edit || '/Profile/Index?handler=Edit';

    editBtn.addEventListener('click', () => modal.show());

    $('#saveProfileBtn').addEventListener('click', () => {
        const form = $('#editProfileForm');
        if (!form) return;

        form.classList.add('was-validated');
        if (!form.checkValidity()) return;

        const data = Object.fromEntries(new FormData(form).entries());

        fetch(editUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        })
            .then(async res => { if (!res.ok) throw new Error(await res.text()); return res.json().catch(() => ({})); })
            .then(() => {
                const nameEl = document.querySelector('.display-name');
                const emailStrong = document.querySelector('.identity .muted strong');
                if (nameEl && data.DisplayName) nameEl.textContent = data.DisplayName;
                if (emailStrong && data.Email) emailStrong.textContent = data.Email;

                showToast('บันทึกโปรไฟล์แล้ว');
                modal.hide();
            })
            .catch(err => {
                showToast('บันทึกไม่สำเร็จ');
                console.error(err);
            });
    });
})();

// ===== Favorites (Grid like IG) =====
let favLoaded = false;
async function loadFavorites() {
    if (favLoaded) return;
    favLoaded = true;

    const grid = $('#favGrid');
    const skel = $('#favSkeleton');
    const empty = $('#favEmpty');
    if (!grid) return;

    const EP = window.ProfileEndpoints || {};
    try {
        skel?.classList.remove('d-none');
        grid.innerHTML = '';
        empty.classList.add('d-none');

        const res = await fetch(EP.myFavorites || '/api/me/favorites', { credentials: 'include' });
        if (!res.ok) throw new Error(`GET ${res.status}`);
        const items = await res.json();

        if (!items || !items.length) {
            empty.classList.remove('d-none');
            return;
        }

        grid.innerHTML = items.map(cardTemplate).join('');
        bindFavButtons(grid);
    } catch (err) {
        console.error(err);
        empty.classList.remove('d-none');
        empty.textContent = 'โหลดรายการถูกใจไม่สำเร็จ';
    } finally {
        skel?.classList.add('d-none');
    }
}

function cardTemplate(it) {
    const img = it.imageUrl || 'https://via.placeholder.com/600x600?text=%F0%9F%8D%BA';
    const rating = (typeof it.rating === 'number' ? it.rating.toFixed(1) : (it.rating || 0));
    const rc = it.ratingCount || 0;
    const province = it.province || '';
    const price = (typeof it.price === 'number') ? fmtTHB(it.price) : (it.price || '');

    return `
    <div class="card">
      <a class="media" href="/Detail?id=${it.id}" title="${escapeHtml(it.name || 'ดูรายละเอียด')}">
        <img src="${escapeAttr(img)}" alt="${escapeAttr(it.name || 'image')}">
        ${province ? `<div class="pill">${escapeHtml(province)}</div>` : ``}
        <button type="button" class="fav-btn" data-id="${it.id}" title="เอาออกจากถูกใจ">♥</button>
      </a>
      <div class="body">
        <div class="title" title="${escapeAttr(it.name || '')}">${escapeHtml(it.name || '')}</div>
        <div class="sub">★ ${rating} (${rc})${price ? ` · ฿${price}` : ''}</div>
      </div>
    </div>
  `;
}
function bindFavButtons(root) {
    const EP = window.ProfileEndpoints || {};
    $$('.fav-btn', root).forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault(); e.stopPropagation();
            const id = Number(btn.getAttribute('data-id'));
            if (!Number.isInteger(id) || id <= 0) return;
            if (!confirm('เอาออกจากรายการถูกใจหรือไม่?')) return;
            try {
                const res = await fetch(EP.toggleFavorite?.(id) || `/api/beers/${id}/favorite`, { method: 'DELETE', credentials: 'include' });
                if (!res.ok && res.status !== 204) throw new Error(await res.text());
                btn.closest('.card')?.remove();
                if (!$('#favGrid')?.children.length) { $('#favEmpty')?.classList.remove('d-none'); }
                showToast('เอาออกจากถูกใจแล้ว');
            } catch (err) { console.error(err); showToast('เอาออกไม่สำเร็จ'); }
        });
    });
}
function escapeHtml(s) { const d = document.createElement('div'); d.innerText = s ?? ""; return d.innerHTML; }
function escapeAttr(s) { return String(s ?? '').replace(/"/g, '&quot;'); }

// ===== Activity Refresh (bind once when tab open) =====
let activityBound = false;
function attachActivityOnce() {
    if (activityBound) return;
    activityBound = true;
    const btn = $('#refreshActivityBtn');
    const list = $('#activityList');
    const empty = $('#activityEmpty');
    const EP = window.ProfileEndpoints || {};
    if (!btn || !list) return;

    btn.addEventListener('click', async () => {
        btn.disabled = true; btn.textContent = 'กำลังโหลด...';
        try {
            const res = await fetch(EP.activities || '/Profile/Index?handler=Activities', { credentials: 'include' });
            if (!res.ok) throw new Error(await res.text());
            const items = await res.json();
            list.innerHTML = '';
            if (!items || !items.length) { empty?.classList.remove('d-none'); return; }
            empty?.classList.add('d-none');
            items.forEach(txt => {
                const li = document.createElement('li');
                li.className = 'item';
                li.innerHTML = `<span class="dot"></span><div class="content"></div>`;
                li.querySelector('.content').textContent = txt;
                list.appendChild(li);
            });
        } catch (err) {
            console.error(err);
            showToast('โหลดกิจกรรมไม่สำเร็จ');
        } finally {
            btn.disabled = false; btn.textContent = 'รีเฟรช';
        }
    });
}
