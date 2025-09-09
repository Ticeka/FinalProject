// ====== Read data from embedded JSON ======
const dataEl = document.getElementById('locationsData');
const locations = dataEl ? JSON.parse(dataEl.textContent || '[]') : [];

// ====== Map Setup ======
const map = L.map('map', { scrollWheelZoom: true, worldCopyJump: true }).setView([15.87, 100.9925], 6);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap contributors' }).addTo(map);

// Cluster group
const markerCluster = L.markerClusterGroup({ showCoverageOnHover: false, spiderfyOnMaxZoom: true });
map.addLayer(markerCluster);

// ====== Emoji Pins ======
function getEmojiByType(type) {
    const t = (type || '').toLowerCase();
    if (t.includes('ไวน์') || t.includes('wine')) return { emoji: '🍷', cls: 'poi-wine' };
    if (t.includes('เบียร์') || t.includes('beer') || t.includes('ลาเกอร์') || t.includes('ipa')) return { emoji: '🍺', cls: 'poi-beer' };
    if (t.includes('วิสกี้') || t.includes('เหล้า') || t.includes('rum') || t.includes('whisky') || t.includes('liquor')) return { emoji: '🥃', cls: 'poi-liquor' };
    return { emoji: '🍶', cls: 'poi-default' };
}
function createEmojiIcon(type) {
    const { emoji, cls } = getEmojiByType(type);
    return L.divIcon({
        html: `<div class="poi-pin ${cls}">${emoji}</div>`,
        className: '', iconSize: [32, 32], iconAnchor: [16, 16], popupAnchor: [0, -14]
    });
}

// ====== UI Elements ======
const $search = document.getElementById('searchText');
const $province = document.getElementById('provinceSelect');
const $type = document.getElementById('typeSelect');
const $minAbv = document.getElementById('minAbv');
const $total = document.getElementById('totalCount');
const $reset = document.getElementById('resetBtn');
const $searchBtn = document.getElementById('searchBtn');
const $locateBtn = document.getElementById('locateBtn');
const $chips = document.querySelectorAll('.chip-btn');
const $resultList = document.getElementById('resultList');
const $loadMore = document.getElementById('loadMoreBtn');
const $shareBtn = document.getElementById('shareBtn');

const formatPrice = p => (p == null ? '–' : new Intl.NumberFormat('th-TH', { style: 'currency', currency: 'THB', maximumFractionDigits: 0 }).format(p));
const starRating = r => r == null ? '<span class="text-muted">–</span>' : '★'.repeat(Math.round(r)) + '☆'.repeat(5 - Math.round(r)) + ` <span class="text-muted">(${r.toFixed(1)})</span>`;
const debounce = (fn, wait = 300) => { let t; return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), wait); }; };

// ====== Renderers ======
let idToMarker = new Map(); // sync list <-> marker
function buildPopup(loc) {
    const alc = loc.AlcoholLevel != null ? `${loc.AlcoholLevel}%` : '–';
    return `
    <div class="popup">
      <div class="popup-header">${loc.Name ?? 'ไม่ทราบชื่อ'}</div>
      <div class="small text-muted mb-2">${loc.Type ?? '-'} • ${loc.Province ?? '-'}</div>
      <div class="mb-2">${loc.Description ?? ''}</div>
      <div class="d-flex flex-wrap gap-2 mb-2">
        <span class="chip">แอลกอฮอล์: <strong>${alc}</strong></span>
        <span class="chip">ราคา: <strong>${formatPrice(loc.Price)}</strong></span>
      </div>
      <div class="mb-2">คะแนน: ${starRating(loc.Rating)} <span class="text-muted">${loc.RatingCount ? loc.RatingCount + ' รีวิว' : ''}</span></div>
      <div class="d-grid"><a href="/Detail?id=${loc.Id}" class="btn btn-sm btn-outline-primary">ดูรายละเอียด</a></div>
    </div>`;
}

function renderMarkers(data) {
    markerCluster.clearLayers();
    idToMarker.clear();
    const group = [];

    data.forEach(loc => {
        if (loc.Latitude == null || loc.Longitude == null) return;
        if (loc.Latitude === 0 && loc.Longitude === 0) return; // กันพิกัด 0,0
        const marker = L.marker([loc.Latitude, loc.Longitude], { icon: createEmojiIcon(loc.Type) })
            .bindPopup(buildPopup(loc))
            .bindTooltip(loc.Name || '', { direction: 'top' });
        markerCluster.addLayer(marker);
        idToMarker.set(loc.Id, marker);
        group.push(marker);
    });

    if (group.length) {
        if (group.length === 1) {
            map.setView(group[0].getLatLng(), 13);
        } else {
            const g = L.featureGroup(group);
            map.fitBounds(g.getBounds().pad(0.2));
        }
    }
    $total.textContent = data.length.toString();
}

// ====== Side List ======
const pageSize = 12;
let filtered = [];
function renderListPage(items, start = 0) {
    const slice = items.slice(start, start + pageSize);
    if (start === 0) $resultList.innerHTML = '';
    slice.forEach(loc => {
        const { emoji } = getEmojiByType(loc.Type);
        const li = document.createElement('li');
        li.className = 'list-group-item d-flex align-items-center gap-3 list-row';
        li.tabIndex = 0;
        li.dataset.id = loc.Id;

        li.innerHTML = `
      <div class="poi-pin poi-mini">${emoji}</div>
      <div class="flex-grow-1">
        <div class="fw-semibold">${loc.Name ?? '-'}</div>
        <div class="text-muted">${loc.Type ?? '-'} • ${loc.Province ?? '-'}</div>
      </div>
      <div class="text-end small">
        <div class="text-muted">${loc.AlcoholLevel != null ? loc.AlcoholLevel + '%' : '–'}</div>
        <div class="fw-semibold">${formatPrice(loc.Price)}</div>
      </div>
    `;

        // hover -> pulse marker
        li.addEventListener('mouseenter', () => {
            const m = idToMarker.get(loc.Id);
            if (!m) return;
            const el = m._icon?.firstChild;
            if (el) { el.classList.add('pulse'); setTimeout(() => el.classList.remove('pulse'), 600); }
        });

        // click -> fly & open popup
        li.addEventListener('click', () => {
            const m = idToMarker.get(loc.Id);
            if (!m) return;
            map.flyTo(m.getLatLng(), 14, { duration: .5 });
            m.openPopup();
        });

        // Enter -> go detail
        li.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') { window.location.href = `/Detail?id=${loc.Id}`; }
        });

        $resultList.appendChild(li);
    });

    // toggle load more
    if (items.length > start + pageSize) {
        $loadMore.classList.remove('d-none');
        $loadMore.onclick = () => renderListPage(items, start + pageSize);
    } else {
        $loadMore.classList.add('d-none');
        $loadMore.onclick = null;
    }
}

// ====== Filtering ======
function getFilter() {
    const keyword = ($search.value || '').trim().toLowerCase();
    const province = $province.value;
    const type = $type.value;
    const minAbv = parseFloat($minAbv.value);
    return { keyword, province, type, minAbv: isNaN(minAbv) ? -Infinity : minAbv };
}

function doFilter() {
    const { keyword, province, type, minAbv } = getFilter();

    const out = locations.filter(loc => {
        const name = (loc.Name || '').toLowerCase();
        const desc = (loc.Description || '').toLowerCase();
        const matchText = !keyword || name.includes(keyword) || desc.includes(keyword);
        const matchProvince = !province || loc.Province === province;
        const matchType = !type
            || (loc.Type || '') === type
            || (type === 'เหล้า' && /(วิสกี้|เหล้า|rum|whisky|liquor)/i.test(loc.Type || ''));
        const matchAbv = (loc.AlcoholLevel ?? 0) >= minAbv;
        return matchText && matchProvince && matchType && matchAbv;
    });

    filtered = out;
    renderMarkers(out);
    renderListPage(out, 0);

    if (out.length === 0) showToast('ไม่พบผลลัพธ์ ลองเปลี่ยนคำค้นหาหรือปรับตัวกรอง');
    updateShareUrl();
}

function resetFilters() {
    $search.value = '';
    $province.value = '';
    $type.value = '';
    $minAbv.value = '';
    filtered = locations.slice();
    renderMarkers(filtered);
    renderListPage(filtered, 0);
    updateShareUrl();
    document.querySelectorAll('.chip-btn').forEach(b => b.classList.remove('active'));
    document.querySelector('.chip-btn[data-type=""]')?.classList.add('active');
}

// ====== Geolocate ======
function locateMe() {
    if (!navigator.geolocation) return showToast('เบราว์เซอร์ไม่รองรับการระบุตำแหน่ง');
    navigator.geolocation.getCurrentPosition((pos) => {
        const { latitude, longitude } = pos.coords;
        map.flyTo([latitude, longitude], 12, { duration: 0.8 });
        L.circle([latitude, longitude], { radius: 400, color: '#0d6efd' }).addTo(map);
    }, () => showToast('ไม่สามารถอ่านตำแหน่งได้'));
}

// ====== Toast ======
let toastTimer;
function showToast(msg) {
    clearTimeout(toastTimer);
    const el = document.getElementById('toast');
    el.textContent = msg;
    el.classList.add('show');
    toastTimer = setTimeout(() => el.classList.remove('show'), 2200);
}

// ====== Quick chips (with active state) ======
document.querySelectorAll('.chip-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.chip-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        $type.value = btn.dataset.type || '';
        doFilter();
    });
});

// ====== Shareable URL ======
function updateShareUrl() {
    const params = new URLSearchParams();
    if ($search.value) params.set('q', $search.value);
    if ($province.value) params.set('prov', $province.value);
    if ($type.value) params.set('type', $type.value);
    if ($minAbv.value) params.set('minAbv', $minAbv.value);
    const qs = params.toString();
    const url = qs ? `${location.pathname}?${qs}` : location.pathname;
    $shareBtn.dataset.url = url;
}
$shareBtn.addEventListener('click', async () => {
    const url = $shareBtn.dataset.url || location.href;
    try { await navigator.clipboard.writeText(location.origin + ($shareBtn.dataset.url || '')); showToast('คัดลอกลิงก์แล้ว!'); }
    catch { showToast('คัดลอกไม่สำเร็จ'); }
});

// ====== Events ======
const deb = (fn) => debounce(fn, 250);
$search.addEventListener('input', deb(doFilter));
$province.addEventListener('change', doFilter);
$type.addEventListener('change', doFilter);
$minAbv.addEventListener('input', deb(doFilter));
$searchBtn.addEventListener('click', doFilter);
$reset.addEventListener('click', resetFilters);
$locateBtn.addEventListener('click', locateMe);
$search.addEventListener('keydown', (e) => { if (e.key === 'Enter') doFilter(); });

// ====== Init ======
(function initFromQuery() {
    const qs = new URLSearchParams(location.search);
    if (qs.get('q')) $search.value = qs.get('q');
    if (qs.get('prov')) $province.value = qs.get('prov');
    if (qs.get('type')) {
        $type.value = qs.get('type');
        document.querySelectorAll('.chip-btn').forEach(b => b.classList.toggle('active', b.dataset.type === qs.get('type')));
    }
    if (qs.get('minAbv')) $minAbv.value = qs.get('minAbv');
})();
filtered = locations.slice();
renderMarkers(filtered);
renderListPage(filtered, 0);
updateShareUrl();