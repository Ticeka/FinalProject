// ===== Helpers =====
const $ = (sel, root = document) => root.querySelector(sel);

function showToast(message) {
    const toastEl = $('#profileToast');
    $('#toastMsg').textContent = message || 'ดำเนินการสำเร็จ';
    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 2200 });
    toast.show();
}

function setUploading(visible) {
    const wrap = $('#uploadProgressWrap');
    if (!wrap) return;
    wrap.classList.toggle('d-none', !visible);
    wrap.setAttribute('aria-hidden', visible ? 'false' : 'true');
}

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
        if (!/^image\/(jpeg|png|webp|gif|bmp)$/i.test(file.type)) {
            showToast('ไฟล์ภาพไม่ถูกต้อง');
            return;
        }
        if (file.size > 3 * 1024 * 1024) {
            showToast('ไฟล์ใหญ่เกิน 3 MB');
            return;
        }

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
                fallback?.replaceWith(img);
            }
            img.src = reader.result;
        };
        reader.readAsDataURL(file);

        // Upload
        const fd = new FormData();
        fd.append('file', file);

        setUploading(true);
        fetch(uploadUrl, { method: 'POST', body: fd })
            .then(async res => {
                if (!res.ok) throw new Error(await res.text());
                return res.json().catch(() => ({}));
            })
            .then(data => {
                setUploading(false);
                $('#uploadProgress').style.width = '100%';
                showToast('อัปโหลดรูปโปรไฟล์เรียบร้อย');
                if (data?.avatarUrl) {
                    const img = $('#avatarPreview');
                    if (img) img.src = data.avatarUrl;
                }
                removeBtn?.removeAttribute('disabled');
            })
            .catch(err => {
                setUploading(false);
                $('#uploadProgress').style.width = '0%';
                showToast('อัปโหลดไม่สำเร็จ');
                console.error(err);
            });
    }

    if (removeBtn) {
        removeBtn.addEventListener('click', () => {
            if (removeBtn.classList.contains('disabled')) return;
            if (!confirm('ลบรูปโปรไฟล์หรือไม่?')) return;

            fetch(removeUrl, { method: 'POST' })
                .then(res => {
                    if (!res.ok) throw new Error('remove failed');
                    return res.json().catch(() => ({}));
                })
                .then(() => {
                    showToast('ลบรูปโปรไฟล์แล้ว');
                    const img = $('#avatarPreview');
                    if (img) {
                        const fallback = document.createElement('div');
                        fallback.id = 'avatarFallback';
                        fallback.className = 'avatar-fallback';
                        const span = document.createElement('span');
                        span.textContent = img.getAttribute('alt-initials')?.trim() || $('#avatarFallback')?.dataset.initials || '?';
                        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
                        svg.setAttribute('class', 'avatar-icon');
                        svg.setAttribute('viewBox', '0 0 24 24');
                        svg.innerHTML = '<circle cx="12" cy="8" r="4"></circle><path d="M4 20a8 8 0 0 1 16 0"></path>';
                        fallback.appendChild(span);
                        fallback.appendChild(svg);
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
            .then(async res => {
                if (!res.ok) throw new Error(await res.text());
                return res.json().catch(() => ({}));
            })
            .then(() => {
                const nameEl = document.querySelector('.display-name');
                const emailText = document.querySelector('.text-muted strong');
                if (nameEl && data.DisplayName) nameEl.textContent = data.DisplayName;
                if (emailText && data.Email) emailText.textContent = data.Email;

                showToast('บันทึกโปรไฟล์แล้ว');
                modal.hide();
            })
            .catch(err => {
                showToast('บันทึกไม่สำเร็จ');
                console.error(err);
            });
    });
})();

// ===== Activity Refresh =====
(function initActivity() {
    const btn = $('#refreshActivityBtn');
    const list = $('#activityList');
    const empty = $('#activityEmpty');
    if (!btn || !list) return;

    const EP = window.ProfileEndpoints || {};
    const actUrl = EP.activities || '/Profile/Index?handler=Activities';

    btn.addEventListener('click', () => {
        btn.disabled = true;
        btn.textContent = 'กำลังโหลด...';
        fetch(actUrl)
            .then(async res => {
                if (!res.ok) throw new Error(await res.text());
                return res.json();
            })
            .then(items => {
                list.innerHTML = '';
                if (!items || !items.length) {
                    empty?.classList.remove('d-none');
                    return;
                }
                empty?.classList.add('d-none');
                items.forEach(txt => {
                    const li = document.createElement('li');
                    li.className = 'activity-item';
                    li.innerHTML = `<span class="dot"></span><div class="content"><div class="title"></div></div>`;
                    li.querySelector('.title').textContent = txt;
                    list.appendChild(li);
                });
            })
            .catch(err => {
                showToast('โหลดกิจกรรมไม่สำเร็จ');
                console.error(err);
            })
            .finally(() => {
                btn.disabled = false;
                btn.textContent = 'รีเฟรช';
            });
    });
})();
