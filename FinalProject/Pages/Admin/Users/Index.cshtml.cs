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

        public UsersIndexVM VM { get; private set; } = new();

        public async Task OnGetAsync()
        {
            VM.Q = q; VM.Role = role; VM.Status = status; VM.Sort = sort; VM.Page = Math.Max(page, 1);

            // ----- เตรียม Role ทั้งหมด -----
            VM.AllRoles = await _roles.Roles.AsNoTracking().Select(r => r.Name!).OrderBy(n => n).ToListAsync();

            // ----- Query ผู้ใช้พื้นฐาน -----
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

            // ----- กรองตามสถานะ -----
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

            // ----- กรอง Role (join AspNetUserRoles) -----
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

            // ----- เรียงลำดับ -----
            query = sort?.ToLower() switch
            {
                "email" => query.OrderBy(u => u.Email),
                "lock" => query.OrderByDescending(u => u.LockoutEnd),
                _ => query.OrderBy(u => u.DisplayName).ThenBy(u => u.UserName),
            };

            // ----- นับทั้งหมด ก่อนตัดหน้า -----
            VM.TotalItems = await query.CountAsync();

            // ----- ตัดหน้า -----
            int pageSize = VM.PageSize;
            int skip = (VM.Page - 1) * pageSize;
            var pageUsers = await query.Skip(skip).Take(pageSize).ToListAsync();

            var userIds = pageUsers.Select(u => u.Id).ToList();

            // ----- ดึง Roles ของผู้ใช้ในหน้านี้ทีเดียว -----
            var rolesJoin = await (
                from ur in _db.UserRoles.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name! }
            ).ToListAsync();

            var roleMap = rolesJoin.GroupBy(x => x.UserId)
                                   .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).OrderBy(n => n).ToList());

            // ----- ดึง UserStats -----
            var stats = await _db.Set<UserStats>().AsNoTracking()
                            .Where(s => userIds.Contains(s.UserId))
                            .ToDictionaryAsync(s => s.UserId, s => s);

            // ----- สร้าง VM -----
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

        // ====== Actions ======
        public async Task<IActionResult> OnPostLockAsync(string id, int days = 7)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(days);
            var res = await _users.UpdateAsync(user);
            TempData["Msg"] = res.Succeeded ? $"Locked {user.UserName} {days} days" : string.Join(";", res.Errors.Select(e => e.Description));
            return RedirectToPage(new { q, role, status, sort, page });
        }

        public async Task<IActionResult> OnPostUnlockAsync(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnd = null;
            var res = await _users.UpdateAsync(user);
            TempData["Msg"] = res.Succeeded ? $"Unlocked {user.UserName}" : string.Join(";", res.Errors.Select(e => e.Description));
            return RedirectToPage(new { q, role, status, sort, page });
        }

        public async Task<IActionResult> OnPostToggle2faAsync(string id, bool enable)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.TwoFactorEnabled = enable;
            var res = await _users.UpdateAsync(user);
            TempData["Msg"] = res.Succeeded ? (enable ? "2FA enabled" : "2FA disabled") : string.Join(";", res.Errors.Select(e => e.Description));
            return RedirectToPage(new { q, role, status, sort, page });
        }

        public async Task<IActionResult> OnPostUpdateRolesAsync(string id, List<string> selectedRoles)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var allRoles = await _roles.Roles.Select(r => r.Name!).ToListAsync();
            var current = await _users.GetRolesAsync(user);

            var toAdd = selectedRoles.Except(current).Where(r => allRoles.Contains(r)).ToList();
            var toRemove = current.Except(selectedRoles).Where(r => allRoles.Contains(r)).ToList();

            if (toAdd.Any())
            {
                var addRes = await _users.AddToRolesAsync(user, toAdd);
                if (!addRes.Succeeded) { TempData["Msg"] = string.Join(";", addRes.Errors.Select(e => e.Description)); return RedirectToPage(new { q, role, status, sort, page }); }
            }
            if (toRemove.Any())
            {
                var rmRes = await _users.RemoveFromRolesAsync(user, toRemove);
                if (!rmRes.Succeeded) { TempData["Msg"] = string.Join(";", rmRes.Errors.Select(e => e.Description)); return RedirectToPage(new { q, role, status, sort, page }); }
            }

            TempData["Msg"] = "Roles updated.";
            return RedirectToPage(new { q, role, status, sort, page });
        }

        // ===== Logs JSON (for modal in Users page) =====
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
            // ใส่ทั้ง 2 ชุด field เพื่อเข้ากันได้กับ JS เก่า/ใหม่
            return new JsonResult(new
            {
                total,
                page,
                pageSize,
                items,
                totalItems = total,
                totalPages
            });
        }
    }
}
