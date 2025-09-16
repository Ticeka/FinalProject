using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.Services;
using static FinalProject.Endpoints.EndpointHelpers;

namespace FinalProject.Endpoints
{
    // DTO เฉพาะคอมเมนต์
    public record CommentCreateDto(string Body, int? ParentId = null);
    public record CommentTreeDto(
        int Id,
        string Body,
        string DisplayName,
        DateTime CreatedAt,
        bool CanDelete,
        string? AvatarUrl,
        int? Rating,
        int? ParentId,
        List<CommentTreeDto> Replies
    );

    public static class CommentsEndpoints
    {
        public static IEndpointRouteBuilder MapCommentsEndpoints(this IEndpointRouteBuilder api)
        {
            // GET /api/beers/{id}/comments
            api.MapGet("/beers/{id:int}/comments", async (
                int id,
                int? skip,
                int? take,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager) =>
            {
                int _skip = Math.Max(0, skip ?? 0);
                int _take = Math.Clamp(take is null or <= 0 ? 200 : take.Value, 1, 500);

                var meId = (ctx.User?.Identity?.IsAuthenticated ?? false) ? userManager.GetUserId(ctx.User) : null;
                var isAdmin = false;
                if (meId != null)
                {
                    var me = await userManager.GetUserAsync(ctx.User);
                    var roles = me is null ? Array.Empty<string>() : await userManager.GetRolesAsync(me);
                    isAdmin = roles.Contains("Admin");
                }

                var all = await db.BeerComments.AsNoTracking()
                    .Where(c => c.LocalBeerId == id && !c.IsDeleted)
                    .OrderBy(c => c.CreatedAt)
                    .Skip(_skip).Take(_take)
                    .Select(c => new
                    {
                        c.Id,
                        c.Body,
                        c.UserId,
                        c.DisplayName,
                        c.UserName,
                        c.CreatedAt,
                        c.ParentId
                    })
                    .ToListAsync();

                var userIds = all.Where(r => r.UserId != null).Select(r => r.UserId!).Distinct().ToList();
                var userMap = await db.Users.AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                var dict = new Dictionary<int, CommentTreeDto>(all.Count);
                var roots = new List<CommentTreeDto>();

                foreach (var r in all)
                {
                    ApplicationUser? u = null;
                    if (r.UserId != null) userMap.TryGetValue(r.UserId!, out u);

                    var display = u != null
                        ? (u.DisplayName ?? u.UserName ?? MaskEmail(u.Email))
                        : (r.DisplayName ?? "User");

                    var avatar = u?.AvatarUrl;
                    var canDelete = isAdmin || (meId != null && r.UserId == meId);

                    var node = new CommentTreeDto(
                        Id: r.Id,
                        Body: r.Body,
                        DisplayName: display,
                        CreatedAt: r.CreatedAt,
                        CanDelete: canDelete,
                        AvatarUrl: avatar,
                        Rating: null,
                        ParentId: r.ParentId,
                        Replies: new List<CommentTreeDto>());

                    dict[r.Id] = node;
                }

                foreach (var node in dict.Values)
                {
                    if (node.ParentId is int pid && dict.TryGetValue(pid, out var parent))
                        parent.Replies.Add(node);
                    else
                        roots.Add(node);
                }

                void sortRec(List<CommentTreeDto> list)
                {
                    list.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
                    foreach (var n in list) sortRec(n.Replies);
                }
                sortRec(roots);

                return Results.Ok(roots);
            })
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK);

            // POST /api/beers/{id}/comments
            api.MapPost("/beers/{id:int}/comments", async (
                int id,
                [FromBody] CommentCreateDto dto,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false))
                    return Results.Unauthorized();

                if (dto is null || string.IsNullOrWhiteSpace(dto.Body))
                    return Results.BadRequest("comment is required");

                if (dto.Body.Length > 1000)
                    return Results.BadRequest("comment too long");

                var beer = await db.LocalBeers.FindAsync(id);
                if (beer is null) return Results.NotFound("beer not found");

                if (dto.ParentId is int pid)
                {
                    var parentOk = await db.BeerComments
                        .AnyAsync(c => c.Id == pid && c.LocalBeerId == id && !c.IsDeleted);
                    if (!parentOk) return Results.BadRequest("invalid parent");
                }

                var me = await userManager.GetUserAsync(ctx.User);
                var uid = me?.Id;
                var uname = me?.UserName;

                var cmt = new BeerComment
                {
                    LocalBeerId = id,
                    Body = dto.Body.Trim(),
                    DisplayName = null,
                    UserId = uid,
                    UserName = uname,
                    IpHash = GetIpHash(ctx),
                    CreatedAt = DateTime.UtcNow,
                    ParentId = dto.ParentId
                };

                db.BeerComments.Add(cmt);
                await db.SaveChangesAsync();

                var ua = ctx.Request.Headers.UserAgent.ToString();
                await logger.LogAsync(uid, "comment.add", "LocalBeer", id.ToString(), Trunc(cmt.Body, 120), null, cmt.IpHash, ua);

                return Results.Created($"/api/beers/{id}/comments/{cmt.Id}", new { id = cmt.Id });
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

            // DELETE /api/beers/{beerId}/comments/{commentId}
            api.MapDelete("/beers/{beerId:int}/comments/{commentId:int}", async (
                int beerId, int commentId,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false))
                    return Results.Unauthorized();

                var cmt = await db.BeerComments.FirstOrDefaultAsync(c => c.Id == commentId && c.LocalBeerId == beerId);
                if (cmt is null) return Results.NotFound();

                var me = await userManager.GetUserAsync(ctx.User);
                var uid = me?.Id ?? "";
                var roles = me is null ? Array.Empty<string>() : await userManager.GetRolesAsync(me);
                var isAdmin = roles.Contains("Admin");

                if (!(isAdmin || (cmt.UserId != null && cmt.UserId == uid)))
                    return Results.Forbid();

                cmt.IsDeleted = true;
                if (!string.IsNullOrWhiteSpace(cmt.Body)) cmt.Body = "[ลบแล้ว]";
                await db.SaveChangesAsync();

                var ua = ctx.Request.Headers.UserAgent.ToString();
                await logger.LogAsync(uid, "comment.remove", "LocalBeer", beerId.ToString(), Trunc(cmt.Body ?? "", 120), null, cmt.IpHash, ua);

                return Results.NoContent();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

            return api;
        }
    }
}
