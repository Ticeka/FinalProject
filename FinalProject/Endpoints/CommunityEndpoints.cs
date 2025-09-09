using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.Endpoints;
using FinalProject.Services; // <<-- เพิ่ม

namespace FinalProject.Endpoints
{
    public static class CommunityEndpoints
    {
        const string FP_COOKIE = "qr_fp";

        public static IEndpointRouteBuilder MapCommunityEndpoints(this IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/api");

            // --- Quick Ratings ---
            g.MapPost("/ratings/quick", async (
                [FromBody] QuickRateDto dto,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>               // <<-- เพิ่ม
            {
                if (dto is null) return Results.BadRequest("payload is required");
                if (dto.Score < 1 || dto.Score > 5) return Results.BadRequest("score must be 1..5");

                var beer = await db.LocalBeers.FindAsync(dto.BeerId);
                if (beer is null) return Results.NotFound();

                if (!ctx.Request.Cookies.TryGetValue(FP_COOKIE, out var deviceId) || string.IsNullOrWhiteSpace(deviceId))
                {
                    deviceId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
                    ctx.Response.Cookies.Append(FP_COOKIE, deviceId, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = ctx.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = true,
                        Expires = DateTimeOffset.UtcNow.AddYears(5)
                    });
                }

                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var ipHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip)));
                var ua = ctx.Request.Headers.UserAgent.ToString();

                var exists = await db.QuickRatings.AsNoTracking()
                    .AnyAsync(r => r.LocalBeerId == dto.BeerId && r.Fingerprint == deviceId);
                if (exists) return Results.Conflict("already rated by this device");

                db.QuickRatings.Add(new QuickRating
                {
                    LocalBeerId = dto.BeerId,
                    Score = dto.Score,
                    IpHash = ipHash,
                    Fingerprint = deviceId,
                    CreatedAt = DateTime.UtcNow
                });

                var total = (beer.Rating * beer.RatingCount) + dto.Score;
                beer.RatingCount += 1;
                beer.Rating = Math.Round(total / Math.Max(beer.RatingCount, 1), 2);

                try { await db.SaveChangesAsync(); }
                catch (DbUpdateException) { return Results.Conflict("already rated by this device"); }

                // ----- LOG: rating.add -----
                string? uid = ctx.User?.Identity?.IsAuthenticated == true ? userManager.GetUserId(ctx.User) : null;
                await logger.LogAsync(uid, "rating.add", "LocalBeer", dto.BeerId.ToString(), $"score {dto.Score}", null, ipHash, ua);

                return Results.Ok(new { avg = beer.Rating, count = beer.RatingCount });
            })
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

            // --- Comments helper ---
            static string GetIpHash(HttpContext ctx)
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip)));
            }
            static string Trunc(string s, int n) => string.IsNullOrEmpty(s) ? s : (s.Length <= n ? s : s.Substring(0, n));

            // --- Comments: GET ---
            g.MapGet("/beers/{id:int}/comments", async (int id, int skip, int take, AppDbContext db, HttpContext ctx, UserManager<ApplicationUser> userManager) =>
            {
                skip = Math.Max(0, skip);
                take = Math.Clamp(take <= 0 ? 20 : take, 1, 100);

                string? meId = (ctx.User?.Identity?.IsAuthenticated ?? false)
                    ? userManager.GetUserId(ctx.User) : null;

                var raw = await db.BeerComments.AsNoTracking()
                    .Where(c => !c.IsDeleted && c.LocalBeerId == id)
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip).Take(take)
                    .Select(c => new { c.Id, c.Body, c.DisplayName, c.UserId, c.UserName, c.CreatedAt })
                    .ToListAsync();

                var userIds = raw.Where(r => r.UserId != null).Select(r => r.UserId!).Distinct().ToList();
                var userMap = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

                static string MaskEmail(string? email) => string.IsNullOrWhiteSpace(email) ? "User" : email.Split('@')[0];

                var items = raw.Select(r =>
                {
                    ApplicationUser? u = null;
                    if (r.UserId != null) userMap.TryGetValue(r.UserId!, out u);

                    var disp = (u != null) ? (u.DisplayName ?? u.UserName ?? MaskEmail(u.Email)) : (r.DisplayName ?? "Guest");
                    var avatarUrl = u?.AvatarUrl;
                    var canDelete = (meId != null && r.UserId == meId);
                    int? rating = null;

                    return new CommentView(r.Id, r.Body, disp, r.CreatedAt, canDelete, disp, avatarUrl, rating);
                }).ToList();

                return Results.Ok(items);
            })
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK);

            // --- Comments: POST ---
            g.MapPost("/beers/{id:int}/comments", async (
                int id,
                [FromBody] NewCommentDto dto,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>                  // <<-- เพิ่ม
            {
                if (dto is null || string.IsNullOrWhiteSpace(dto.Body)) return Results.BadRequest("comment is required");
                if (dto.Body.Length > 1000) return Results.BadRequest("comment too long");

                var beer = await db.LocalBeers.FindAsync(id);
                if (beer is null) return Results.NotFound("beer not found");

                string? uid = null, uname = null, display = dto.DisplayName?.Trim();
                ApplicationUser? me = null;

                if (ctx.User?.Identity?.IsAuthenticated == true)
                {
                    me = await userManager.GetUserAsync(ctx.User);
                    uid = me?.Id; uname = me?.UserName;
                }
                else
                {
                    display = string.IsNullOrWhiteSpace(display) ? "Guest" : display;
                    if (display.Length > 100) display = display[..100];
                }

                var cmt = new BeerComment
                {
                    LocalBeerId = id,
                    Body = dto.Body.Trim(),
                    DisplayName = uid is null ? display : null,
                    UserId = uid,
                    UserName = uname,
                    IpHash = GetIpHash(ctx),
                    CreatedAt = DateTime.UtcNow
                };

                db.BeerComments.Add(cmt);
                await db.SaveChangesAsync();

                // ----- LOG: comment.add -----
                var ua = ctx.Request.Headers.UserAgent.ToString();
                await logger.LogAsync(uid, "comment.add", "LocalBeer", id.ToString(), Trunc(cmt.Body, 120), null, cmt.IpHash, ua);

                static string MaskEmail(string? email) => string.IsNullOrWhiteSpace(email) ? "User" : email.Split('@')[0];
                var disp = me != null ? (me.DisplayName ?? me.UserName ?? MaskEmail(me.Email)) : (cmt.DisplayName ?? "Guest");
                var avatarUrl = me?.AvatarUrl;
                int? rating = null;

                var view = new CommentView(cmt.Id, cmt.Body, disp, cmt.CreatedAt, true, disp, avatarUrl, rating);
                return Results.Created($"/api/beers/{id}/comments/{cmt.Id}", view);
            })
            .AllowAnonymous()
            .Produces<CommentView>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

            // --- Comments: DELETE ---
            g.MapDelete("/beers/{beerId:int}/comments/{commentId:int}", async (
                int beerId, int commentId,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>                   // <<-- เพิ่ม
            {
                var cmt = await db.BeerComments.FirstOrDefaultAsync(c => c.Id == commentId && c.LocalBeerId == beerId);
                if (cmt is null) return Results.NotFound();

                var isAuth = ctx.User?.Identity?.IsAuthenticated == true;
                var uid = isAuth ? userManager.GetUserId(ctx.User) : null;
                var isAdmin = isAuth && (await userManager.GetRolesAsync(await userManager.GetUserAsync(ctx.User))).Contains("Admin");

                if (!isAdmin)
                {
                    if (cmt.UserId != null)
                    {
                        if (!isAuth) return Results.Unauthorized();
                        if (cmt.UserId != uid) return Results.Forbid();
                    }
                    else
                    {
                        var ipHash = GetIpHash(ctx);
                        if (!string.Equals(cmt.IpHash, ipHash, StringComparison.OrdinalIgnoreCase)) return Results.Forbid();
                        if ((DateTime.UtcNow - cmt.CreatedAt) > TimeSpan.FromDays(1)) return Results.Forbid();
                    }
                }

                db.BeerComments.Remove(cmt);
                await db.SaveChangesAsync();

                // ----- LOG: comment.remove (จะโชว์เมื่อเลือก All actions) -----
                var ua = ctx.Request.Headers.UserAgent.ToString();
                await logger.LogAsync(uid, "comment.remove", "LocalBeer", beerId.ToString(), Trunc(cmt.Body ?? "", 120), null, cmt.IpHash, ua);

                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

            // --- Favorites ---
            g.MapGet("/beers/{id:int}/favorite", async (int id, AppDbContext db, HttpContext ctx, UserManager<ApplicationUser> userManager) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);
                var isFav = await db.BeerFavorites.AsNoTracking().AnyAsync(f => f.LocalBeerId == id && f.UserId == uid);
                return Results.Ok(new { isFavorite = isFav });
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

            g.MapPost("/beers/{id:int}/favorite", async (
                int id,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>                  // <<-- เพิ่ม
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);

                var beer = await db.LocalBeers.FindAsync(id);
                if (beer is null) return Results.NotFound("beer not found");

                var exists = await db.BeerFavorites.AnyAsync(f => f.LocalBeerId == id && f.UserId == uid);
                if (exists) return Results.Ok(new { ok = true });

                db.BeerFavorites.Add(new BeerFavorite { UserId = uid!, LocalBeerId = id });

                var stats = await db.UserStats.FindAsync(uid) ?? new UserStats { UserId = uid!, Reviews = 0, Favorites = 0, Badges = 0 };
                if (stats.UserId == uid && db.Entry(stats).State == EntityState.Detached) db.UserStats.Add(stats);
                stats.Favorites = Math.Max(0, stats.Favorites + 1);

                await db.SaveChangesAsync();

                // ----- LOG: favorite.add -----
                var ua = ctx.Request.Headers.UserAgent.ToString();
                await logger.LogAsync(uid, "favorite.add", "LocalBeer", id.ToString(), $"Favorited {beer.Name}", null, GetIpHash(ctx), ua);

                return Results.Ok(new { ok = true });
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

            g.MapDelete("/beers/{id:int}/favorite", async (
                int id,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>                  // <<-- เพิ่ม
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);

                var fav = await db.BeerFavorites.FirstOrDefaultAsync(f => f.LocalBeerId == id && f.UserId == uid);
                if (fav != null) db.BeerFavorites.Remove(fav);

                var stats = await db.UserStats.FindAsync(uid);
                if (stats != null) stats.Favorites = Math.Max(0, stats.Favorites - 1);

                await db.SaveChangesAsync();

                // ----- LOG: favorite.remove -----
                var beerName = await db.LocalBeers.AsNoTracking().Where(b => b.Id == id).Select(b => b.Name).FirstOrDefaultAsync() ?? $"beer#{id}";
                var ua = ctx.Request.Headers.UserAgent.ToString();
                await logger.LogAsync(uid, "favorite.remove", "LocalBeer", id.ToString(), $"Unfavorited {beerName}", null, GetIpHash(ctx), ua);

                return Results.NoContent();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

            // --- Me/Favorites ---
            g.MapGet("/me/favorites", async (AppDbContext db, HttpContext ctx, UserManager<ApplicationUser> userManager) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);

                var items = await db.BeerFavorites.AsNoTracking()
                    .Where(f => f.UserId == uid)
                    .OrderByDescending(f => f.CreatedAt)
                    .Join(db.LocalBeers, f => f.LocalBeerId, b => b.Id, (f, b) => new
                    {
                        id = b.Id,
                        name = b.Name,
                        province = b.Province,
                        imageUrl = b.ImageUrl,
                        type = b.Type,
                        rating = b.Rating,
                        ratingCount = b.RatingCount,
                        price = b.Price
                    })
                    .ToListAsync();

                return Results.Ok(items);
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

            // --- debug whoami ---
            g.MapGet("/debug/whoami", (HttpContext ctx) =>
            {
                var u = ctx.User;
                return Results.Ok(new
                {
                    isAuth = u?.Identity?.IsAuthenticated ?? false,
                    name = u?.Identity?.Name,
                    claims = u?.Claims.Select(c => new { c.Type, c.Value })
                });
            });

            return app;
        }
    }
}
