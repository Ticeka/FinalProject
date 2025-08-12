// ===== Helpers =====
const $ = s => document.querySelector(s);
const $$ = s => Array.from(document.querySelectorAll(s));
const debounce = (fn, t = 250) => { let id; return (...a) => { clearTimeout(id); id = setTimeout(() => fn(...a), t); } };

// ===== Elements =====
const searchInput = $('#searchInput');
const provinceSelect = $('#provinceSelect');
const typeSelect = $('#typeSelect');
const alcoholSelect = $('#alcoholSelect');
const sortSelect = $('#sortSelect');
const resetBtn = $('#resetBtn');
const clearSearch = $('#clearSearch');
const resultCount = $('#resultCount');
const listEl = $('#beerList');
const pagerEl = $('#pager');

const pageSize = 10;   // ✅ 10 รายการต่อหน้า
let currentPage = 1;
let filtered = [];

function collect() { return $$('#beerList .beer-item'); }

// ===== Read/Sync page from querystring =====
function getPageFromQS() {
    const qs = new URLSearchParams(location.search);
    const p = parseInt(qs.get('page') || '1', 10);
    return isNaN(p) || p < 1 ? 1 : p;
}
function setPageToQS(p) {
    const qs = new URLSearchParams(location.search);
    if (p <= 1) { qs.delete('page'); }
    else { qs.set('page', String(p)); }
    const url = `${location.pathname}${qs.toString() ? '?' + qs.toString() : ''}`;
    history.replaceState(null, '', url);
}

// ===== Filters =====
function getFilters() {
    const kw = (searchInput?.value || '').trim().toLowerCase();
    const prov = provinceSelect?.value || '';
    const type = typeSelect?.value || '';
    const alc = alcoholSelect?.value || '';
    return { kw, prov, type, alc };
}

function applyFilters() {
    const { kw, prov, type, alc } = getFilters();
    const items = collect();

    filtered = items.filter(el => {
        const name = (el.dataset.name || '').toLowerCase();
        const desc = (el.dataset.desc || '').toLowerCase();
        const okKw = !kw || name.includes(kw) || desc.includes(kw);
        const okProv = !prov || el.dataset.province === prov;
        const okType = !type || el.dataset.type === type;
        const okAlc = !alc || el.dataset.alc === alc; // exact
        return okKw && okProv && okType && okAlc;
    });

    // sort
    const val = sortSelect?.value || '';
    if (val) {
        const [key, dir] = val.split('-');
        filtered.sort((a, b) => {
            const pa = parseFloat(a.dataset.price || '0');
            const pb = parseFloat(b.dataset.price || '0');
            const aa = parseFloat(a.dataset.alc || '0');
            const ab = parseFloat(b.dataset.alc || '0');
            const na = (a.dataset.name || '').toLowerCase();
            const nb = (b.dataset.name || '').toLowerCase();
            let d = 0;
            if (key === 'price') d = pa - pb;
            if (key === 'alc') d = aa - ab;
            if (key === 'name') d = na.localeCompare(nb);
            return dir === 'desc' ? -d : d;
        });
        // re-append order in DOM
        const parent = listEl;
        filtered.forEach(el => parent.appendChild(el));
    }

    // format price text (keep data-* สำหรับ logic)
    $$('#beerList .beer-item .price').forEach(span => {
        const parent = span.closest('.beer-item');
        const v = parseFloat(parent?.dataset.price || '0');
        span.textContent = v > 0 ? new Intl.NumberFormat('th-TH').format(v) : '–';
    });

    // ไปหน้าจาก query (ถ้ามี) เมื่อเปลี่ยนฟิลเตอร์ให้กลับหน้า 1
    currentPage = 1;
    renderPage(currentPage);
    resultCount.textContent = `${filtered.length} รายการ`;
}

// ===== Pagination =====
function renderPage(page) {
    const total = filtered.length;
    const totalPages = Math.max(1, Math.ceil(total / pageSize));

    // clamp page
    currentPage = Math.min(Math.max(1, page), totalPages);

    // hide all
    collect().forEach(el => el.style.display = 'none');

    // show current slice
    const start = (currentPage - 1) * pageSize;
    const end = start + pageSize;
    filtered.slice(start, end).forEach(el => el.style.display = '');

    renderPager(totalPages);
    setPageToQS(currentPage);
}

function renderPager(totalPages) {
    if (!pagerEl) return;
    if (totalPages <= 1) { pagerEl.innerHTML = ''; return; }

    // windowed numbers
    const maxNums = 7;
    const nums = [];
    let from = Math.max(1, currentPage - Math.floor(maxNums / 2));
    let to = Math.min(totalPages, from + maxNums - 1);
    from = Math.max(1, Math.min(from, to - maxNums + 1));
    for (let i = from; i <= to; i++) nums.push(i);

    let html = '';
    const prevDisabled = currentPage === 1 ? ' disabled' : '';
    const nextDisabled = currentPage === totalPages ? ' disabled' : '';

    html += `<li class="page-item${prevDisabled}"><a class="page-link" href="#" data-pg="${currentPage - 1}" aria-label="ก่อนหน้า">Prev</a></li>`;

    if (from > 1) {
        html += `<li class="page-item"><a class="page-link" href="#" data-pg="1">1</a></li>`;
        if (from > 2) html += `<li class="page-item disabled"><span class="page-link">…</span></li>`;
    }

    nums.forEach(i => {
        const active = i === currentPage ? ' active' : '';
        const aria = i === currentPage ? ' aria-current="page"' : '';
        html += `<li class="page-item${active}"><a class="page-link" href="#" data-pg="${i}"${aria}>${i}</a></li>`;
    });

    if (to < totalPages) {
        if (to < totalPages - 1) html += `<li class="page-item disabled"><span class="page-link">…</span></li>`;
        html += `<li class="page-item"><a class="page-link" href="#" data-pg="${totalPages}">${totalPages}</a></li>`;
    }

    html += `<li class="page-item${nextDisabled}"><a class="page-link" href="#" data-pg="${currentPage + 1}" aria-label="ถัดไป">Next</a></li>`;
    pagerEl.innerHTML = html;
}

// pager clicks (delegation)
pagerEl?.addEventListener('click', e => {
    const a = e.target.closest('[data-pg]');
    if (!a) return;
    e.preventDefault();
    const pg = parseInt(a.dataset.pg, 10);
    if (!isNaN(pg)) {
        renderPage(pg);
        // scroll to list
        const y = listEl.getBoundingClientRect().top + window.scrollY - 80;
        window.scrollTo({ top: y, behavior: 'smooth' });
    }
});

// ===== Events =====
const deb = fn => debounce(fn, 180);
searchInput?.addEventListener('input', deb(applyFilters));
provinceSelect?.addEventListener('change', applyFilters);
typeSelect?.addEventListener('change', applyFilters);
alcoholSelect?.addEventListener('change', applyFilters);
sortSelect?.addEventListener('change', applyFilters);

clearSearch?.addEventListener('click', () => { searchInput.value = ''; applyFilters(); });
resetBtn?.addEventListener('click', () => {
    searchInput.value = ''; provinceSelect.value = ''; typeSelect.value = ''; alcoholSelect.value = ''; sortSelect.value = '';
    applyFilters();
});

// ===== Init =====
document.addEventListener('DOMContentLoaded', () => {
    applyFilters();
    const pageQS = getPageFromQS();
    if (pageQS > 1) renderPage(pageQS);
});
