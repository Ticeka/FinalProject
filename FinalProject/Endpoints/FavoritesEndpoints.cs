using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.Services;
using static FinalProject.Endpoints.EndpointHelpers;

namespace FinalProject.Endpoints
{
    public static class FavoritesEndpoints
    {
        public static IEndpointRouteBuilder MapFavoritesEndpoints(this IEndpointRouteBuilder api)
        {
            // GET /api/beers/{id}/favorite
            api.MapGet("/beers/{id:int}/favorite", async (int id, AppDbContext db, HttpContext ctx, UserManager<ApplicationUser> userManager) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);
                var isFav = await db.BeerFavorites.AsNoTracking().AnyAsync(f => f.LocalBeerId == id && f.UserId == uid);
                return Results.Ok(new { isFavorite = isFav });
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

            // POST /api/beers/{id}/favorite
            api.MapPost("/beers/{id:int}/favorite", async (
                int id,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>
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

                var ua = ctx.Request.Headers.UserAgent.ToString();
                await logger.LogAsync(uid, "favorite.add", "LocalBeer", id.ToString(), $"Favorited {beer.Name}", null, GetIpHash(ctx), ua);

                return Results.Ok(new { ok = true });
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

            // DELETE /api/beers/{id}/favorite
            api.MapDelete("/beers/{id:int}/favorite", async (
                int id,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);

                var fav = await db.BeerFavorites.FirstOrDefaultAsync(f => f.LocalBeerId == id && f.UserId == uid);
                if (fav != null) db.BeerFavorites.Remove(fav);

                var stats = await db.UserStats.FindAsync(uid);
                if (stats != null) stats.Favorites = Math.Max(0, stats.Favorites - 1);

                await db.SaveChangesAsync();

                var beerName = await db.LocalBeers.AsNoTracking().Where(b => b.Id == id).Select(b => b.Name).FirstOrDefaultAsync() ?? $"beer#{id}";
                var ua = ctx.Request.Headers.UserAgent.ToString();
                await logger.LogAsync(uid, "favorite.remove", "LocalBeer", id.ToString(), $"Unfavorited {beerName}", null, GetIpHash(ctx), ua);

                return Results.NoContent();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);
            // GET /api/me/favorites  -> คืนลิสต์รายการที่ผู้ใช้กดถูกใจ
            api.MapGet("/me/favorites", async (
                int? skip,
                int? take,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);
                if (string.IsNullOrWhiteSpace(uid)) return Results.Unauthorized();

                int _skip = Math.Max(0, skip ?? 0);
                int _take = Math.Clamp(take is null or <= 0 ? 60 : take.Value, 1, 200);

                // join ให้ชัวร์ ไม่พึ่ง navigation property
                var items = await db.BeerFavorites
                    .AsNoTracking()
                    .Where(f => f.UserId == uid)
                    .OrderByDescending(f => f.Id)
                    .Skip(_skip).Take(_take)
                    .Join(db.LocalBeers,
                        f => f.LocalBeerId,
                        b => b.Id,
                        (f, b) => new
                        {
                            id = b.Id,
                            name = b.Name,
                            province = b.Province,
                            district = b.District,
                            type = b.Type,
                            imageUrl = b.ImageUrl,
                            rating = b.Rating,
                            ratingCount = b.RatingCount
                        })
                    .ToListAsync();

                return Results.Ok(items); // คืนเป็น array ตรง ๆ ให้ JS ใช้ง่าย
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);


            return api;
        }
    }
}
