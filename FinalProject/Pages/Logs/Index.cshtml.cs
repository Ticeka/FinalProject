using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Pages.Admin.Logs
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db) { _db = db; }

        // ===== Filters (bound for keeping form values) =====
        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public string? userId { get; set; }
        [BindProperty(SupportsGet = true)] public string? action { get; set; }
        [BindProperty(SupportsGet = true)] public string? subjectType { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? dateFrom { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? dateTo { get; set; }

        public void OnGet() { /* page only */ }

        // ===== List (JSON) =====
        public async Task<IActionResult> OnGetListAsync(int page = 1, int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _db.ActivityLogs.AsNoTracking().Include(l => l.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(l =>
                    (l.Message != null && l.Message.Contains(term)) ||
                    (l.SubjectId != null && l.SubjectId.Contains(term)) ||
                    (l.MetaJson != null && l.MetaJson.Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var uid = userId.Trim();
                query = query.Where(l => l.UserId == uid);
            }
            if (!string.IsNullOrWhiteSpace(action))
            {
                var act = action.Trim();
                query = query.Where(l => l.Action == act);
            }
            if (!string.IsNullOrWhiteSpace(subjectType))
            {
                var st = subjectType.Trim();
                query = query.Where(l => l.SubjectType == st);
            }
            if (dateFrom.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
                query = query.Where(l => l.CreatedAt >= fromUtc);
            }
            if (dateTo.HasValue)
            {
                // inclusive to the end of day (UTC)
                var toUtc = DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(l => l.CreatedAt <= toUtc);
            }

            query = query.OrderByDescending(l => l.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.CreatedAt,
                    l.UserId,
                    userName = l.User != null ? l.User.UserName : null,
                    userDisplayName = l.User != null ? l.User.DisplayName : null,
                    l.Action,
                    l.SubjectType,
                    l.SubjectId,
                    l.Message,
                    l.MetaJson
                })
                .ToListAsync();

            return new JsonResult(new { total, page, pageSize, items });
        }

        // ===== Export CSV =====
        public async Task<IActionResult> OnGetExportAsync()
        {
            var query = _db.ActivityLogs.AsNoTracking().Include(l => l.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(l =>
                    (l.Message != null && l.Message.Contains(term)) ||
                    (l.SubjectId != null && l.SubjectId.Contains(term)) ||
                    (l.MetaJson != null && l.MetaJson.Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var uid = userId.Trim();
                query = query.Where(l => l.UserId == uid);
            }
            if (!string.IsNullOrWhiteSpace(action))
            {
                var act = action.Trim();
                query = query.Where(l => l.Action == act);
            }
            if (!string.IsNullOrWhiteSpace(subjectType))
            {
                var st = subjectType.Trim();
                query = query.Where(l => l.SubjectType == st);
            }
            if (dateFrom.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
                query = query.Where(l => l.CreatedAt >= fromUtc);
            }
            if (dateTo.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(l => l.CreatedAt <= toUtc);
            }

            query = query.OrderByDescending(l => l.CreatedAt);

            var rows = await query
                .Select(l => new
                {
                    l.Id,
                    l.CreatedAt,
                    User = l.User != null ? (l.User.DisplayName ?? l.User.UserName) : l.UserId,
                    l.Action,
                    l.SubjectType,
                    l.SubjectId,
                    l.Message
                })
                .ToListAsync();

            var csv = "Id,CreatedAt,User,Action,SubjectType,SubjectId,Message\r\n";
            foreach (var r in rows)
            {
                string esc(string? s) => s == null ? "" : "\"" + s.Replace("\"", "\"\"") + "\"";
                csv += $"{r.Id},{r.CreatedAt:O},{esc(r.User)},{esc(r.Action)},{esc(r.SubjectType)},{esc(r.SubjectId)},{esc(r.Message)}\r\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var name = $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return File(bytes, "text/csv", name);
        }
    }
}
