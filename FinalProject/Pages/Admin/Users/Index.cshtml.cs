using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public IndexModel(AppDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
        {
            _db = db;
            _users = users;
            _roles = roles;
        }

        [BindProperty(SupportsGet = true)] public string? q { get; set; }
        [BindProperty(SupportsGet = true)] public string? role { get; set; }
        [BindProperty(SupportsGet = true)] public string? status { get; set; }
        [BindProperty(SupportsGet = true)] public string? sort { get; set; }
        [BindProperty(SupportsGet = true)] public int page { get; set; } = 1;

        [TempData] public string? Msg { get; set; }
        [TempData] public string? Err { get; set; }

        public UsersIndexVM VM { get; private set; } = new();

        public async Task OnGetAsync()
        {
            VM.Q = q; VM.Role = role; VM.Status = status; VM.Sort = sort; VM.Page = Math.Max(page, 1);

            VM.AllRoles = await _roles.Roles.AsNoTracking()
                .Select(r => r.Name!).OrderBy(n => n).ToListAsync();

            var query = _db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim().ToLower();
                query = query.Where(u =>
                    (u.UserName ?? "").ToLower().Contains(kw) ||
                    (u.Email ?? "").ToLower().Contains(kw) ||
                    (u.DisplayName ?? "").ToLower().Contains(kw)
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "locked":
                        query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
                        break;
                    case "active":
                        query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow);
                        break;
                    case "2fa":
                        query = query.Where(u => u.TwoFactorEnabled);
                        break;
                    case "unconfirmed":
                        query = query.Where(u => !u.EmailConfirmed);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleEntity = await _roles.FindByNameAsync(role);
                if (roleEntity != null)
                {
                    var userIdsInRole = await _db.UserRoles
                        .Where(ur => ur.RoleId == roleEntity.Id)
                        .Select(ur => ur.UserId)
                        .ToListAsync();

                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }
            }

            query = sort?.ToLower() switch
            {
                "email" => query.OrderBy(u => u.Email),
                "lock" => query.OrderByDescending(u => u.LockoutEnd),
                _ => query.OrderBy(u => u.DisplayName).ThenBy(u => u.UserName),
            };

            VM.TotalItems = await query.CountAsync();

            int pageSize = VM.PageSize;
            int skip = (VM.Page - 1) * pageSize;
            var pageUsers = await query.Skip(skip).Take(pageSize).ToListAsync();

            var userIds = pageUsers.Select(u => u.Id).ToList();

            var rolesJoin = await (
                from ur in _db.UserRoles.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name! }
            ).ToListAsync();

            var roleMap = rolesJoin.GroupBy(x => x.UserId)
                                   .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).OrderBy(n => n).ToList());

            var stats = await _db.Set<UserStats>().AsNoTracking()
                            .Where(s => userIds.Contains(s.UserId))
                            .ToDictionaryAsync(s => s.UserId, s => s);

            VM.Items = pageUsers.Select(u => new UserRowVM
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                UserName = u.UserName,
                Email = u.Email,
                EmailConfirmed = u.EmailConfirmed,
                PhoneNumber = u.PhoneNumber,
                TwoFactorEnabled = u.TwoFactorEnabled,
                LockoutEnd = u.LockoutEnd,
                LockoutEnabled = u.LockoutEnabled,
                Reviews = stats.TryGetValue(u.Id, out var st) ? st.Reviews : 0,
                Comments = stats.TryGetValue(u.Id, out st) ? st.Comments : 0,
                Favorites = stats.TryGetValue(u.Id, out st) ? st.Favorites : 0,
                Badges = stats.TryGetValue(u.Id, out st) ? st.Badges : 0,
                Roles = roleMap.TryGetValue(u.Id, out var rs) ? rs : new List<string>()
            }).ToList();
        }

        // ---- Helper: ถ้า Accept เป็น JSON จะส่ง JSON; ถ้าไม่ใช่ ให้ Redirect กลับ ./Index พร้อมตัวกรองเดิม
        private IActionResult JsonOrRedirect(object payload, string okMsg, string? errMsg = null)
        {
            var accept = Request.Headers["Accept"].ToString();
            var wantsJson = accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (wantsJson) return new JsonResult(payload);

            if (errMsg is null) Msg = okMsg; else Err = errMsg;

            // ระบุชื่อหน้าให้ชัดเจน ป้องกัน No page named ''
            return RedirectToPage("./Index", new { q = this.q, role = this.role, status = this.status, sort = this.sort, page = this.page });
        }

        // ===== Actions =====
        public async Task<IActionResult> OnPostLockAsync(string id, int days = 7)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
                return JsonOrRedirect(new { ok = false, message = "User not found." }, "", "User not found.");

            var errors = new List<string>();

            var enRes = await _users.SetLockoutEnabledAsync(user, true);
            if (!enRes.Succeeded) errors.AddRange(enRes.Errors.Select(e => e.Description));

            var endRes = await _users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(days));
            if (!endRes.Succeeded) errors.AddRange(endRes.Errors.Select(e => e.Description));

            var ok = errors.Count == 0;
            var message = ok ? $"Locked {user.UserName} {days} days" : string.Join("; ", errors);

            return JsonOrRedirect(new
            {
                ok,
                message,
                isLocked = ok,
                lockoutEndLocal = ok ? DateTimeOffset.UtcNow.AddDays(days).ToLocalTime().ToString("yyyy-MM-dd HH:mm") : null
            }, ok ? message : "", ok ? null : message);
        }

        public async Task<IActionResult> OnPostUnlockAsync(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
                return JsonOrRedirect(new { ok = false, message = "User not found." }, "", "User not found.");

            var errors = new List<string>();

            var enRes = await _users.SetLockoutEnabledAsync(user, true);
            if (!enRes.Succeeded) errors.AddRange(enRes.Errors.Select(e => e.Description));

            var endRes = await _users.SetLockoutEndDateAsync(user, null);
            if (!endRes.Succeeded) errors.AddRange(endRes.Errors.Select(e => e.Description));

            await _users.ResetAccessFailedCountAsync(user);

            var ok = errors.Count == 0;
            var message = ok ? $"Unlocked {user.UserName}" : string.Join("; ", errors);

            return JsonOrRedirect(new
            {
                ok,
                message,
                isLocked = false,
                lockoutEndLocal = (string?)null
            }, ok ? message : "", ok ? null : message);
        }

        public async Task<IActionResult> OnPostToggle2faAsync(string id, bool enable)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
                return JsonOrRedirect(new { ok = false, message = "User not found." }, "", "User not found.");

            var res = await _users.SetTwoFactorEnabledAsync(user, enable);
            var ok = res.Succeeded;
            var message = ok ? (enable ? "2FA enabled" : "2FA disabled") : string.Join("; ", res.Errors.Select(e => e.Description));

            return JsonOrRedirect(new { ok, message, twoFactorEnabled = enable }, ok ? message : "", ok ? null : message);
        }

        public async Task<IActionResult> OnPostUpdateRolesAsync(string id, List<string>? selectedRoles)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
                return JsonOrRedirect(new { ok = false, message = "User not found." }, "", "User not found.");

            selectedRoles ??= new List<string>();

            var allRoles = await _roles.Roles.Select(r => r.Name!).ToListAsync();
            var current = await _users.GetRolesAsync(user);

            var toAdd = selectedRoles.Except(current).Where(r => allRoles.Contains(r)).ToList();
            var toRemove = current.Except(selectedRoles).Where(r => allRoles.Contains(r)).ToList();

            var errors = new List<string>();

            if (toAdd.Any())
            {
                var addRes = await _users.AddToRolesAsync(user, toAdd);
                if (!addRes.Succeeded) errors.AddRange(addRes.Errors.Select(e => e.Description));
            }
            if (toRemove.Any())
            {
                var rmRes = await _users.RemoveFromRolesAsync(user, toRemove);
                if (!rmRes.Succeeded) errors.AddRange(rmRes.Errors.Select(e => e.Description));
            }

            var ok = errors.Count == 0;
            var message = ok ? "Roles updated." : string.Join("; ", errors);

            var newRoles = await _users.GetRolesAsync(user);

            return JsonOrRedirect(new { ok, message, roles = newRoles }, ok ? message : "", ok ? null : message);
        }

        // ===== Logs JSON =====
        public async Task<IActionResult> OnGetLogsAsync(
            string userId,
            int page = 1,
            int pageSize = 20,
            string? action = null,
            string? q = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new JsonResult(new { total = 0, page = 1, pageSize, items = Array.Empty<object>(), totalItems = 0, totalPages = 1 });

            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _db.Set<ActivityLog>()
                           .AsNoTracking()
                           .Include(l => l.User)
                           .Where(l => l.UserId == userId);

            if (!string.IsNullOrWhiteSpace(action))
            {
                var act = action.Trim();
                query = query.Where(l => l.Action == act);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(l =>
                    (l.Message != null && l.Message.Contains(term)) ||
                    (l.SubjectId != null && l.SubjectId.Contains(term)) ||
                    (l.MetaJson != null && l.MetaJson.Contains(term)));
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
                    l.Message
                })
                .ToListAsync();

            var totalPages = Math.Max(1, (int)Math.Ceiling((double)total / pageSize));
            return new JsonResult(new { total, page, pageSize, items, totalItems = total, totalPages });
        }
    }
}
