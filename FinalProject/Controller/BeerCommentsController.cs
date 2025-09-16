using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Controllers
{
    [ApiController]
    public class BeerCommentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public BeerCommentsController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        static string MaskEmail(string? email) => string.IsNullOrWhiteSpace(email) ? "User" : email.Split('@')[0];

        // GET: /api/beers/{beerId}/comments?skip=0&take=20
        // ดึง "ราก" (ParentId == null) ตาม skip/take และแนบลูกหนึ่งเลเยอร์ (Replies)
        [HttpGet("/api/beers/{beerId:int}/comments")]
        public async Task<IActionResult> GetComments(int beerId, int skip = 0, int take = 20)
        {
            skip = Math.Max(0, skip);
            take = Math.Clamp(take <= 0 ? 20 : take, 1, 100);

            var meId = _userManager.GetUserId(User);
            var isAdmin = User?.IsInRole("Admin") ?? false;

            // 1) ราก
            var roots = await _db.Set<BeerComment>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.LocalBeerId == beerId && c.ParentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip).Take(take)
                .ToListAsync();

            var rootIds = roots.Select(r => r.Id).ToList();

            // 2) ลูกของราก
            var replies = await _db.Set<BeerComment>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.LocalBeerId == beerId && c.ParentId != null && rootIds.Contains(c.ParentId.Value))
                .OrderBy(c => c.CreatedAt) // ลูกเรียงเก่า->ใหม่
                .ToListAsync();

            // 3) โหลดโปรไฟล์ครั้งเดียว
            var userIds = roots.Concat(replies).Where(c => c.UserId != null).Select(c => c.UserId!).Distinct().ToList();
            var userMap = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            CommentOutDto MapNode(BeerComment c)
            {
                ApplicationUser? u = null;
                if (c.UserId != null) userMap.TryGetValue(c.UserId!, out u);

                var display = u != null ? (u.DisplayName ?? u.UserName ?? MaskEmail(u.Email))
                                        : (c.DisplayName ?? "User");

                var avatar = u?.AvatarUrl;
                var profileUrl = u != null ? $"/Profile?id={Uri.EscapeDataString(u.Id)}" : null;
                var canDelete = isAdmin || (meId != null && c.UserId == meId);

                return new CommentOutDto(
                    Id: c.Id,
                    DisplayName: display,
                    Body: c.Body,
                    CreatedAt: c.CreatedAt,
                    CanDelete: canDelete,
                    AvatarUrl: avatar,
                    Rating: null,
                    ProfileUrl: profileUrl,
                    ParentId: c.ParentId,
                    Replies: new List<CommentOutDto>()
                );
            }

            var rootDtos = roots.Select(MapNode).ToList();
            var dict = rootDtos.ToDictionary(x => x.Id);

            foreach (var child in replies)
            {
                var node = MapNode(child);
                if (child.ParentId is int pid && dict.TryGetValue(pid, out var parent))
                    parent.Replies.Add(node);
            }

            // เรียง Replies เก่า->ใหม่
            foreach (var r in rootDtos)
                r.Replies.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));

            return Ok(rootDtos);
        }

        // POST: /api/beers/{beerId}/comments  (บังคับล็อกอิน)
        [Authorize]
        [HttpPost("/api/beers/{beerId:int}/comments")]
        public async Task<IActionResult> CreateComment(int beerId, [FromBody] CommentCreateDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Body))
                return BadRequest("Body is required.");
            if (dto.Body.Length > 1000)
                return BadRequest("Body too long.");

            // ตรวจว่ามีเบียร์จริง
            var beerExists = await _db.LocalBeers.AsNoTracking().AnyAsync(b => b.Id == beerId);
            if (!beerExists) return NotFound("Beer not found.");

            // ถ้าเป็น reply ตรวจ parent ให้อยู่เบียร์เดียวกันและไม่ถูกลบ
            if (dto.ParentId is int pid)
            {
                var parentOk = await _db.Set<BeerComment>()
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == pid && c.LocalBeerId == beerId && !c.IsDeleted);
                if (!parentOk) return BadRequest("Invalid parent.");
            }

            var me = await _userManager.GetUserAsync(User);
            if (me is null) return Unauthorized();

            var c = new BeerComment
            {
                LocalBeerId = beerId,
                Body = dto.Body.Trim(),
                CreatedAt = DateTime.UtcNow,
                UserId = me.Id,
                UserName = me.UserName,
                DisplayName = null,      // เมื่อบังคับล็อกอิน ไม่ใช้ DisplayName ของ guest
                ParentId = dto.ParentId
            };

            _db.Add(c);
            await _db.SaveChangesAsync();
            return StatusCode(201, new { id = c.Id });
        }

        // DELETE: /api/beers/{beerId}/comments/{id}  (แอดมินลบได้ทุกคอมเมนต์ / เจ้าของลบของตัวเอง)
        [Authorize]
        [HttpDelete("/api/beers/{beerId:int}/comments/{id:int}")]
        public async Task<IActionResult> DeleteComment(int beerId, int id)
        {
            var meId = _userManager.GetUserId(User);
            var isAdmin = User?.IsInRole("Admin") ?? false;

            var c = await _db.Set<BeerComment>()
                .FirstOrDefaultAsync(x => x.Id == id && x.LocalBeerId == beerId && !x.IsDeleted);
            if (c == null) return NotFound();

            if (!(isAdmin || (meId != null && c.UserId == meId)))
                return Forbid();

            // soft-delete + บังข้อความ
            c.IsDeleted = true;
            if (!string.IsNullOrWhiteSpace(c.Body)) c.Body = "[ลบแล้ว]";
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
