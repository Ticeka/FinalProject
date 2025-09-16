// admin-users.js — AJAX submit + BIG toast + pending toast + busy + roles/logs modal + live update roles
(function () {
  const $ = (sel, root=document) => root.querySelector(sel);
  const $$ = (sel, root=document) => Array.from(root.querySelectorAll(sel));

  // ===== Toast helpers (ใหญ่ ชัด) =====
  function showToast({ title="แจ้งเตือน", message="", variant="success", delay=3500 }) {
    const area = $("#toastArea");
    if (!area) return alert((title? title+": " : "") + message);

    const el = document.createElement("div");
    el.className = "toast toast-big align-items-center border-0";
    el.setAttribute("role", "status");
    el.setAttribute("aria-live", "polite");
    el.setAttribute("aria-atomic", "true");

    const bgHead = variant === "danger" ? "bg-danger text-white"
                 : variant === "warning" ? "bg-warning"
                 : variant === "info"    ? "bg-info text-white"
                 : "bg-success text-white";

    const icon = variant === "danger" ? "⛔"
               : variant === "warning" ? "⚠️"
               : variant === "info"    ? "ℹ️"
               : "✅";

    el.innerHTML = `
      <div class="toast-header ${bgHead}">
        <span class="toast-icon">${icon}</span>
        <strong class="me-auto">${title}</strong>
        <small>now</small>
        <button type="button" class="btn-close ${bgHead.includes('text-white')?'btn-close-white':''} ms-2 mb-1" data-bs-dismiss="toast" aria-label="Close"></button>
      </div>
      <div class="toast-body">${message}</div>
    `;
    area.appendChild(el);

    // eslint-disable-next-line no-undef
    const t = new bootstrap.Toast(el, { delay });
    t.show();
    el.addEventListener("hidden.bs.toast", () => el.remove());
    return { el, t };
  }

  function showPending(message="กำลังดำเนินการ…") {
    return showToast({
      title: "กำลังดำเนินการ",
      message: `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>${message}`,
      variant: "info",
      delay: 10000
    });
  }

  // ===== Flash from TempData -> Toast =====
  (function flashToToast() {
    const flash = $("#flash");
    if (!flash) return;
    const msg = flash.dataset.msg?.trim();
    const err = flash.dataset.err?.trim();
    if (msg) showToast({ title: "สำเร็จ", message: msg, variant: "success" });
    if (err) showToast({ title: "ไม่สำเร็จ", message: err, variant: "danger", delay: 6000 });

    $$(".badge.bg-info-subtle, .badge.bg-danger-subtle").forEach(b => b.classList.add("d-none"));
  })();

  // ===== Busy state =====
  function setBusy(btn, busy=true, text="กำลังดำเนินการ..."){
    if (!btn) return;
    if (busy){
      if (btn.disabled) return;
      btn.disabled = true;
      btn.dataset._orig = btn.innerHTML;
      btn.innerHTML = `<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>${text}`;
    } else {
      btn.disabled = false;
      if (btn.dataset._orig) btn.innerHTML = btn.dataset._orig;
    }
  }

  // ===== AJAX submit for .ajax-form =====
  $$(".ajax-form").forEach(form => {
    form.addEventListener("submit", async (ev) => {
      const btn = form.querySelector("button[type='submit'], button:not([type])");
      const confirmText = btn?.getAttribute("data-confirm");
      if (confirmText && !confirm(confirmText)) {
        ev.preventDefault();
        ev.stopPropagation();
        return false;
      }

      const busyText = btn?.getAttribute("data-busy") || "กำลังดำเนินการ...";
      const pendingText = btn?.getAttribute("data-pending") || "กำลังส่งคำสั่งไปยังเซิร์ฟเวอร์";
      const pendingToast = showPending(pendingText);
      setBusy(btn, true, busyText);

      ev.preventDefault();
      ev.stopPropagation();

      try {
        const fd = new FormData(form); // รวม anti-forgery token แล้ว
        const res = await fetch(form.action, {
          method: "POST",
          body: fd,
          headers: { "Accept": "application/json" }
        });

        const text = await res.text();
        let data;
        try { data = JSON.parse(text); } catch { data = { ok: res.ok, message: text }; }

        // ปิด toast pending
        try { pendingToast.t.hide(); } catch {}

        if (!res.ok || data.ok === false) {
          const msg = data && data.message ? data.message : `HTTP ${res.status}`;
          showToast({ title: "ไม่สำเร็จ", message: msg, variant: "danger", delay: 6500 });
          setBusy(btn, false);
          return;
        }

        // สำเร็จ: อัปเดต UI ในกรณี UpdateRoles ให้เห็นทันที
        const handler = (form.dataset.handler || "").toLowerCase();
        if (handler === "updateroles" && data.roles && $("#rolesModalUserId")) {
          const userId = $("#rolesModalUserId").value;
          const cell = document.getElementById(`rolesCell-${userId}`);
          if (cell) {
            const roles = Array.isArray(data.roles) ? data.roles : [];
            if (roles.length === 0) {
              cell.innerHTML = `<span class="badge-ghost">no role</span>`;
            } else {
              cell.innerHTML = roles.map(r => `<span class="badge-ghost me-1">${escapeHtml(String(r))}</span>`).join("");
            }
          }
          // ปิด modal เพื่อให้ feedback ชัด
          // eslint-disable-next-line no-undef
          const modal = bootstrap.Modal.getInstance($("#rolesModal"));
          if (modal) modal.hide();
        }

        // Toast สำเร็จ (ใหญ่ ชัด)
        const msg = data.message || "ดำเนินการสำเร็จ";
        showToast({ title: "สำเร็จ", message: msg, variant: "success", delay: 4000 });

        // รีเฟรชเพื่อ sync ทั้งหน้า (เลื่อนเวลาเล็กน้อย)
        setTimeout(() => location.reload(), 900);
      } catch (err) {
        try { pendingToast.t.hide(); } catch {}
        showToast({ title: "ไม่สำเร็จ", message: String(err), variant: "danger", delay: 6500 });
        setBusy(btn, false);
      }
    }, { capture: true });
  });

  // ===== Roles Modal (เติมข้อมูลตอนเปิด) =====
  const rolesModalEl = $("#rolesModal");
  if (rolesModalEl) {
    rolesModalEl.addEventListener("show.bs.modal", (e) => {
      const btn = e.relatedTarget;
      if (!btn) return;
      $("#rolesModalUser").textContent = btn.getAttribute("data-username") || "";
      $("#rolesModalUserId").value = btn.getAttribute("data-userid") || "";
      const roles = (btn.getAttribute("data-roles") || "").split(",").map(s => s.trim()).filter(Boolean);
      $$("#rolesModal input[name='selectedRoles']").forEach(cb => cb.checked = roles.includes(cb.value));
    });
  }

  // ===== Logs =====
  const logsModal = $("#logsModal");
  const getById = (id) => document.getElementById(id);

  let logsState = { userId: "", page: 1, pageSize: 20, total: 0, totalPages: 1 };

  function setSpinner(show) { const sp = getById("logsSpinner"); if (sp) sp.classList.toggle("d-none", !show); }
  function escapeHtml(s){ return (s||"").replace(/[&<>"']/g, m => ({ "&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#039;" }[m])); }
  function readFilters(){
    return {
      action:   getById("logsFilterAction")?.value || "",
      dateFrom: getById("logsFilterFrom")?.value || "",
      dateTo:   getById("logsFilterTo")?.value || "",
      q:        getById("logsFilterQ")?.value?.trim() || ""
    };
  }
  function summaryText(f){
    const parts = [];
    parts.push(`Action: ${f.action || "All"}`);
    if (f.dateFrom || f.dateTo) parts.push(`Date: ${f.dateFrom || "…"} → ${f.dateTo || "…"}`);
    if (f.q) parts.push(`Search: “${f.q}”`);
    return parts.join(" • ");
  }
  function fmtUtc(iso){
    if (!iso) return "-";
    try { const d = new Date(iso); return d.toISOString().replace("T"," ").substring(0,19)+"Z"; }
    catch { return iso; }
  }
  function renderLogs(resp, filters){
    const tbody = getById("logsTableBody");
    const pageInfo = getById("logsPageInfo");
    const summary = getById("logsSummary");
    if (!tbody || !pageInfo || !summary) return;

    const items = resp.items || [];
    tbody.innerHTML = "";
    if (items.length === 0){
      tbody.innerHTML = `<tr><td colspan="4" class="text-center text-muted py-4">No logs.</td></tr>`;
    } else {
      items.forEach(it => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td class="text-nowrap"><div class="small text-muted">${fmtUtc(it.createdAt)}</div></td>
          <td><code class="small">${escapeHtml(it.action || "")}</code></td>
          <td>${escapeHtml(it.message || "")}</td>
          <td class="small text-muted">
            ${escapeHtml(it.subjectType || "-")}
            ${it.subjectId ? `<div class="text-muted">#${escapeHtml(it.subjectId)}</div>` : ""}
          </td>`;
        tbody.appendChild(tr);
      });
    }

    const totalItems = (resp.totalItems != null) ? resp.totalItems : (resp.total != null ? resp.total : 0);
    const pageSize = (resp.pageSize != null) ? resp.pageSize : logsState.pageSize;
    const totalPages = (resp.totalPages != null) ? resp.totalPages : Math.max(1, Math.ceil(totalItems / (pageSize || 20)));

    logsState.page = resp.page || 1;
    logsState.pageSize = pageSize;
    logsState.total = totalItems;
    logsState.totalPages = totalPages;

    const start = totalItems === 0 ? 0 : ((logsState.page - 1) * logsState.pageSize + 1);
    const end = Math.min(logsState.page * logsState.pageSize, totalItems);

    getById("logsPageInfo").textContent = `Page ${logsState.page} / ${logsState.totalPages} • ${totalItems} logs`;
    getById("logsSummary").textContent = `${summaryText(filters)} • ${start}-${end} of ${totalItems}`;

    getById("logsPrevBtn")?.classList.toggle("disabled", logsState.page <= 1);
    getById("logsNextBtn")?.classList.toggle("disabled", logsState.page >= logsState.totalPages);
  }
  async function loadLogs(page = 1){
    if (!logsState.userId) return;
    logsState.page = page;
    setSpinner(true);
    try{
      const f = readFilters();
      const params = new URLSearchParams({
        handler: "Logs",
        userId: logsState.userId,
        page: String(logsState.page),
        pageSize: String(logsState.pageSize)
      });
      if (f.action)  params.set("action",  f.action);
      if (f.dateFrom)params.set("dateFrom",f.dateFrom);
      if (f.dateTo)  params.set("dateTo",  f.dateTo);
      if (f.q)       params.set("q",       f.q);

      const url = `${window.location.pathname}?${params.toString()}`;
      const res = await fetch(url, { headers: { "Accept":"application/json" } });
      const data = await res.json();
      renderLogs(data, f);
    } catch (err){
      console.error(err);
      const tbody = document.getElementById("logsTableBody");
      if (tbody) tbody.innerHTML = `<tr><td colspan="4" class="text-danger">Failed to load logs.</td></tr>`;
      getById("logsPageInfo").textContent = "";
      getById("logsSummary").textContent = "";
      showToast({ title: "ไม่สำเร็จ", message: "โหลด Logs ไม่ได้", variant: "danger", delay: 4500 });
    } finally {
      setSpinner(false);
    }
  }

  // เปิด Logs Modal แล้วโหลดข้อมูล
  const logsModalEl = $("#logsModal");
  if (logsModalEl){
    logsModalEl.addEventListener("show.bs.modal", (ev) => {
      const btn = ev.relatedTarget;
      const userId = btn?.getAttribute("data-userid") || "";
      const userName = btn?.getAttribute("data-username") || "";

      const headerUser = document.getElementById("logsModalUser");
      const hiddenId   = document.getElementById("logsModalUserId");
      if (headerUser) headerUser.textContent = userName;
      if (hiddenId)   hiddenId.value = userId;

      logsState.userId = userId;
      logsState.page = 1;

      document.getElementById("logsTableBody").innerHTML = "";
      document.getElementById("logsPageInfo").textContent = "";
      document.getElementById("logsSummary").textContent = "";

      loadLogs(1);
    });

    document.getElementById("logsPrevBtn")?.addEventListener("click", () => { if (logsState.page > 1) loadLogs(logsState.page - 1); });
    document.getElementById("logsNextBtn")?.addEventListener("click", () => { if (logsState.page < logsState.totalPages) loadLogs(logsState.page + 1); });
    document.getElementById("logsRefreshBtn")?.addEventListener("click", () => { loadLogs(logsState.page || 1); });
    document.getElementById("logsApplyBtn")?.addEventListener("click", () => { loadLogs(1); });
    document.getElementById("logsClearBtn")?.addEventListener("click", () => {
      document.getElementById("logsFilterAction").value = "";
      document.getElementById("logsFilterFrom").value = "";
      document.getElementById("logsFilterTo").value = "";
      document.getElementById("logsFilterQ").value = "";
      loadLogs(1);
    });
    document.getElementById("logsFilterQ")?.addEventListener("keydown", (e) => { if (e.key === "Enter"){ e.preventDefault(); loadLogs(1); } });
  }
})();
