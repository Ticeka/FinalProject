(function () {
    // ===== confirm on danger actions =====
    document.querySelectorAll("[data-confirm]").forEach(btn => {
        btn.addEventListener("click", (e) => {
            const msg = btn.getAttribute("data-confirm") || "Are you sure?";
            if (!confirm(msg)) e.preventDefault();
        });
    });

    // ===== Roles Modal =====
    const rolesModal = document.getElementById("rolesModal");
    if (rolesModal) {
        rolesModal.addEventListener("show.bs.modal", (ev) => {
            const btn = ev.relatedTarget;
            const userId = btn?.getAttribute("data-userid") || "";
            const userName = btn?.getAttribute("data-username") || "";
            const rolesCsv = (btn?.getAttribute("data-roles") || "").trim();
            const current = rolesCsv ? rolesCsv.split(",").map(s => s.trim()) : [];

            const titleSpan = document.getElementById("rolesModalUser");
            const inputId = document.getElementById("rolesModalUserId");
            if (titleSpan) titleSpan.textContent = userName;
            if (inputId) inputId.value = userId;

            rolesModal.querySelectorAll('input[type="checkbox"][name="selectedRoles"]').forEach(chk => {
                const val = chk.value;
                chk.checked = current.includes(val);
            });
        });
    }

    // ===== Logs Modal =====
    const logsModal = document.getElementById("logsModal");
    const $ = (id) => document.getElementById(id);

    let logsState = {
        userId: "",
        page: 1,
        pageSize: 20,
        total: 0,
        totalPages: 1
    };

    function setSpinner(show) {
        const sp = $("logsSpinner");
        if (sp) sp.classList.toggle("d-none", !show);
    }

    function escapeHtml(s) {
        return (s || "").replace(/[&<>"']/g, m => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;"
        }[m]));
    }

    function readFilters() {
        const action = $("logsFilterAction")?.value || "";
        const dateFrom = $("logsFilterFrom")?.value || "";
        const dateTo = $("logsFilterTo")?.value || "";
        const q = $("logsFilterQ")?.value?.trim() || "";
        return { action, dateFrom, dateTo, q };
    }

    function clearFilters() {
        if ($("logsFilterAction")) $("logsFilterAction").value = "";
        if ($("logsFilterFrom")) $("logsFilterFrom").value = "";
        if ($("logsFilterTo")) $("logsFilterTo").value = "";
        if ($("logsFilterQ")) $("logsFilterQ").value = "";
    }

    function summaryText(f) {
        const parts = [];
        parts.push(`Action: ${f.action || "All"}`);
        if (f.dateFrom || f.dateTo) parts.push(`Date: ${f.dateFrom || "…"} → ${f.dateTo || "…"}`);
        if (f.q) parts.push(`Search: “${f.q}”`);
        return parts.join(" • ");
    }

    function fmtUtc(iso) {
        if (!iso) return "-";
        try {
            const d = new Date(iso);
            return d.toISOString().replace("T", " ").substring(0, 19) + "Z";
        } catch { return iso; }
    }

    function renderLogs(resp, filters) {
        const tbody = $("logsTableBody");
        const pageInfo = $("logsPageInfo");
        const summary = $("logsSummary");
        if (!tbody || !pageInfo || !summary) return;

        const items = resp.items || [];
        tbody.innerHTML = "";

        if (items.length === 0) {
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

        // support both totalItems/totalPages and total/pageSize
        const totalItems = (resp.totalItems != null) ? resp.totalItems : (resp.total != null ? resp.total : 0);
        const pageSize = (resp.pageSize != null) ? resp.pageSize : logsState.pageSize;
        const totalPages = (resp.totalPages != null) ? resp.totalPages : Math.max(1, Math.ceil(totalItems / (pageSize || 20)));

        logsState.page = resp.page || 1;
        logsState.pageSize = pageSize;
        logsState.total = totalItems;
        logsState.totalPages = totalPages;

        const start = totalItems === 0 ? 0 : ((logsState.page - 1) * logsState.pageSize + 1);
        const end = Math.min(logsState.page * logsState.pageSize, totalItems);

        pageInfo.textContent = `Page ${logsState.page} / ${logsState.totalPages} • ${totalItems} logs`;
        summary.textContent = `${summaryText(filters)} • ${start}-${end} of ${totalItems}`;

        const prevBtn = $("logsPrevBtn");
        const nextBtn = $("logsNextBtn");
        if (prevBtn) prevBtn.classList.toggle("disabled", logsState.page <= 1);
        if (nextBtn) nextBtn.classList.toggle("disabled", logsState.page >= logsState.totalPages);
    }

    async function loadLogs(page = 1) {
        if (!logsState.userId) return;
        logsState.page = page;
        setSpinner(true);
        try {
            const f = readFilters();
            const params = new URLSearchParams({
                handler: "Logs",
                userId: logsState.userId,
                page: String(logsState.page),
                pageSize: String(logsState.pageSize)
            });
            if (f.action) params.set("action", f.action);
            if (f.dateFrom) params.set("dateFrom", f.dateFrom);
            if (f.dateTo) params.set("dateTo", f.dateTo);
            if (f.q) params.set("q", f.q);

            const url = `${window.location.pathname}?${params.toString()}`;
            const res = await fetch(url, { headers: { "Accept": "application/json" } });
            const text = await res.text();
            if (!res.ok) throw new Error(`HTTP ${res.status}: ${text.substring(0, 200)}`);
            const data = JSON.parse(text);
            renderLogs(data, f);
        } catch (err) {
            console.error(err);
            const tbody = $("logsTableBody");
            if (tbody) tbody.innerHTML = `<tr><td colspan="4" class="text-danger">Failed to load logs. ${escapeHtml(String(err))}</td></tr>`;
            const pageInfo = $("logsPageInfo"); if (pageInfo) pageInfo.textContent = "";
            const summary = $("logsSummary"); if (summary) summary.textContent = "";
        } finally {
            setSpinner(false);
        }
    }

    if (logsModal) {
        logsModal.addEventListener("show.bs.modal", (ev) => {
            const btn = ev.relatedTarget;
            const userId = btn?.getAttribute("data-userid") || "";
            const userName = btn?.getAttribute("data-username") || "";

            const headerUser = $("logsModalUser");
            const hiddenId = $("logsModalUserId");
            if (headerUser) headerUser.textContent = userName;
            if (hiddenId) hiddenId.value = userId;

            logsState.userId = userId;
            logsState.page = 1;

            // reset UI
            if ($("logsTableBody")) $("logsTableBody").innerHTML = "";
            if ($("logsPageInfo")) $("logsPageInfo").textContent = "";
            if ($("logsSummary")) $("logsSummary").textContent = "";
            clearFilters();

            loadLogs(1);
        });

        $("logsPrevBtn")?.addEventListener("click", () => {
            if (logsState.page > 1) loadLogs(logsState.page - 1);
        });

        $("logsNextBtn")?.addEventListener("click", () => {
            if (logsState.page < logsState.totalPages) loadLogs(logsState.page + 1);
        });

        $("logsRefreshBtn")?.addEventListener("click", () => {
            loadLogs(logsState.page || 1);
        });

        $("logsApplyBtn")?.addEventListener("click", () => {
            loadLogs(1);
        });

        $("logsClearBtn")?.addEventListener("click", () => {
            clearFilters();
            loadLogs(1);
        });

        // Enter ในช่อง search ให้ทำงาน
        $("logsFilterQ")?.addEventListener("keydown", (e) => {
            if (e.key === "Enter") {
                e.preventDefault();
                loadLogs(1);
            }
        });
    }
})();