(function () {
    // ===== Utilities =====
    const $ = (s, r = document) => r.querySelector(s);
    const $$ = (s, r = document) => Array.from(r.querySelectorAll(s));

    const state = {
        perPage: 12,
        page: 1,
        items: [],
        filtered: []
    };

    // ===== Init after DOM ready =====
    document.addEventListener('DOMContentLoaded', () => {
        state.items = $$('.beer-item');
        bindFilters();
        bindDeleteGuard();
        applyFilters(); // initial render
    });

    // ===== Filters & Sorting =====
    function bindFilters() {
        const searchInput = $('#searchInput');
        const clearSearch = $('#clearSearch');
        const provinceSelect = $('#provinceSelect');
        const typeSelect = $('#typeSelect');
        const alcoholSelect = $('#alcoholSelect');
        const sortSelect = $('#sortSelect');
        const resetBtn = $('#resetBtn');

        const onChange = () => { state.page = 1; applyFilters(); };

        if (searchInput) searchInput.addEventListener('input', onChange);
        if (provinceSelect) provinceSelect.addEventListener('change', onChange);
        if (typeSelect) typeSelect.addEventListener('change', onChange);
        if (alcoholSelect) alcoholSelect.addEventListener('change', onChange);
        if (sortSelect) sortSelect.addEventListener('change', onChange);

        if (clearSearch) clearSearch.addEventListener('click', () => {
            if (searchInput) searchInput.value = '';
            onChange();
        });

        if (resetBtn) resetBtn.addEventListener('click', () => {
            if (searchInput) searchInput.value = '';
            if (provinceSelect) provinceSelect.value = '';
            if (typeSelect) typeSelect.value = '';
            if (alcoholSelect) alcoholSelect.value = '';
            if (sortSelect) sortSelect.value = '';
            onChange();
        });
    }

    function applyFilters() {
        const q = ($('#searchInput')?.value || '').trim().toLowerCase();
        const province = $('#provinceSelect')?.value || '';
        const type = $('#typeSelect')?.value || '';
        const alc = $('#alcoholSelect')?.value || '';
        const sortBy = $('#sortSelect')?.value || '';

        // filter
        state.filtered = state.items.filter(el => {
            const name = (el.dataset.name || '').toLowerCase();
            const desc = (el.dataset.desc || '').toLowerCase();
            const p = el.dataset.province || '';
            const t = el.dataset.type || '';
            const a = el.dataset.alc || '';
            const hitQ = !q || name.includes(q) || desc.includes(q);
            const hitP = !province || p === province;
            const hitT = !type || t === type;
            const hitA = !alc || a === alc;
            return hitQ && hitP && hitT && hitA;
        });

        // sort
        const parseNum = v => {
            const n = Number(v); return Number.isFinite(n) ? n : 0;
        };

        switch (sortBy) {
            case 'price-asc':
                state.filtered.sort((a, b) => parseNum(a.dataset.price) - parseNum(b.dataset.price)); break;
            case 'price-desc':
                state.filtered.sort((a, b) => parseNum(b.dataset.price) - parseNum(a.dataset.price)); break;
            case 'alc-asc':
                state.filtered.sort((a, b) => parseNum(a.dataset.alc) - parseNum(b.dataset.alc)); break;
            case 'alc-desc':
                state.filtered.sort((a, b) => parseNum(b.dataset.alc) - parseNum(a.dataset.alc)); break;
            case 'name-asc':
                state.filtered.sort((a, b) => (a.dataset.name || '').localeCompare(b.dataset.name || '')); break;
            case 'name-desc':
                state.filtered.sort((a, b) => (b.dataset.name || '').localeCompare(a.dataset.name || '')); break;
            default: /* keep original order */ break;
        }

        updateList();
    }

    // ===== Render & Pagination =====
    function updateList() {
        const list = $('#beerList');
        if (!list) return;

        // hide all then show current page
        state.items.forEach(el => el.style.display = 'none');

        const total = state.filtered.length;
        const totalPages = Math.max(1, Math.ceil(total / state.perPage));
        if (state.page > totalPages) state.page = totalPages;

        const start = (state.page - 1) * state.perPage;
        const pageItems = state.filtered.slice(start, start + state.perPage);
        pageItems.forEach(el => el.style.display = '');

        // count chip
        const countEl = $('#resultCount');
        if (countEl) countEl.textContent = `${total} รายการ`;

        // pager
        renderPager(totalPages);
    }

    function renderPager(totalPages) {
        const pager = $('#pager');
        if (!pager) return;

        const make = (label, page, disabled = false, active = false) => {
            const li = document.createElement('li');
            li.className = `page-item${disabled ? ' disabled' : ''}${active ? ' active' : ''}`;
            const a = document.createElement('a');
            a.className = 'page-link';
            a.href = '#';
            a.textContent = label;
            a.addEventListener('click', (e) => {
                e.preventDefault();
                if (disabled || active) return;
                state.page = page;
                updateList();
            });
            li.appendChild(a);
            return li;
        };

        pager.innerHTML = '';
        const curr = state.page;

        pager.appendChild(make('«', 1, curr === 1));
        pager.appendChild(make('‹', Math.max(1, curr - 1), curr === 1));

        // windowed pages
        const win = 2;
        const totalPagesNum = totalPages;
        const from = Math.max(1, curr - win);
        const to = Math.min(totalPagesNum, curr + win);
        for (let p = from; p <= to; p++) {
            pager.appendChild(make(String(p), p, false, p === curr));
        }

        pager.appendChild(make('›', Math.min(totalPagesNum, curr + 1), curr === totalPagesNum));
        pager.appendChild(make('»', totalPagesNum, curr === totalPagesNum));
    }

    // ===== Delete guard & UX =====
    function bindDeleteGuard() {
        document.addEventListener('submit', (e) => {
            const f = e.target;
            if (!f.classList?.contains('delete-form')) return;
            const btn = f.querySelector('button[type="submit"]');
            if (btn) { btn.disabled = true; btn.textContent = 'กำลังลบ...'; }
        }, true);

        // ป้องกันคลิกภายใน action-bar ไปทริกเกอร์ row click
        $$('.action-bar .btn').forEach(btn => {
            btn.addEventListener('click', (e) => e.stopPropagation());
        });
    }
})();
