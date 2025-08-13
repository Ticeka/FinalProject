using System;
using System.Linq;
using System.Threading.Tasks;
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

        // GET: /api/beers/{beerId}/comments?skip=0&take=20
        [HttpGet("/api/beers/{beerId:int}/comments")]
        public async Task<IActionResult> GetComments(int beerId, int skip = 0, int take = 20)
        {
            var meId = _userManager.GetUserId(User);

            static string MaskEmail(string? email)
                => string.IsNullOrWhiteSpace(email) ? "User" : email.Split('@')[0];

            var query = _db.Set<BeerComment>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.LocalBeerId == beerId)
                .OrderByDescending(c => c.CreatedAt);

            var items = await query
                .Select(c => new
                {
                    c.Id,
                    c.Body,
                    c.CreatedAt,
                    c.UserId,
                    c.DisplayName,
                    c.UserName,
                    User = _db.Users.FirstOrDefault(u => u.Id == c.UserId)
                })
                .ToListAsync();

            var outItems = items.Select(c =>
            {
                var user = c.User;
                var disp = user != null
                    ? (user.DisplayName ?? user.UserName ?? MaskEmail(user.Email))
                    : (c.DisplayName ?? "Guest");

                string? avatarUrl = null;
                if (user != null && !string.IsNullOrWhiteSpace(user.AvatarUrl))
                    avatarUrl = user.AvatarUrl;

                var canDelete = meId != null && (c.UserId == meId ||
                                (User != null && User.IsInRole("Admin")));

                string? profileUrl = user != null ? $"/Profile?id={Uri.EscapeDataString(user.Id)}" : null;

                return new CommentOutDto(
                    Id: c.Id,
                    DisplayName: disp,
                    Body: c.Body,
                    CreatedAt: c.CreatedAt,
                    CanDelete: canDelete,
                    AvatarUrl: avatarUrl,
                    Rating: null,          // ถ้ายังไม่มีคะแนนต่อคอมเมนต์
                    ProfileUrl: profileUrl // <— ส่งลิงก์โปรไฟล์
                );
            });

            return Ok(outItems);
        }

        // POST: /api/beers/{beerId}/comments
        [HttpPost("/api/beers/{beerId:int}/comments")]
        public async Task<IActionResult> CreateComment(int beerId, [FromBody] CommentCreateDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Body))
                return BadRequest("Body is required.");
            if (dto.Body.Length > 1000)
                return BadRequest("Body too long.");

            var me = await _userManager.GetUserAsync(User);

            var c = new BeerComment
            {
                LocalBeerId = beerId,
                Body = dto.Body.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            if (me != null)
            {
                c.UserId = me.Id;
                c.UserName = me.UserName;
            }
            else
            {
                c.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? "Guest" : dto.DisplayName.Trim();
            }

            _db.Add(c);
            await _db.SaveChangesAsync();
            return StatusCode(201);
        }

        // DELETE: /api/beers/{beerId}/comments/{id}
        [Authorize]
        [HttpDelete("/api/beers/{beerId:int}/comments/{id:int}")]
        public async Task<IActionResult> DeleteComment(int beerId, int id)
        {
            var meId = _userManager.GetUserId(User);
            var c = await _db.Set<BeerComment>()
                .FirstOrDefaultAsync(x => x.Id == id && x.LocalBeerId == beerId && !x.IsDeleted);
            if (c == null) return NotFound();

            if (!(meId != null && (c.UserId == meId || User.IsInRole("Admin"))))
                return Forbid();

            c.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
