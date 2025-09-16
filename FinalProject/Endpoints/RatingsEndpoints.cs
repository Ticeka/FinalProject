using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.Services;
using static FinalProject.Endpoints.EndpointHelpers;

namespace FinalProject.Endpoints
{
    // ถ้ามี QuickRateDto อยู่แล้วที่อื่น ให้ลบ record นี้ออก
    // public record QuickRateDto(int BeerId, int Score);

    public static class RatingsEndpoints
    {
        public static IEndpointRouteBuilder MapRatingsEndpoints(this IEndpointRouteBuilder api)
        {
            // POST /api/ratings/quick  (per-user upsert + recalc avg/count)
            api.MapPost("/ratings/quick", async (
                [FromBody] QuickRateDto dto,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager,
                IActivityLogger logger) =>
            {
                if (dto is null) return Results.BadRequest("payload is required");
                if (dto.Score < 1 || dto.Score > 5) return Results.BadRequest("score must be 1..5");

                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);
                if (string.IsNullOrWhiteSpace(uid)) return Results.Unauthorized();

                var beer = await db.LocalBeers.FindAsync(dto.BeerId);
                if (beer is null) return Results.NotFound("beer not found");

                var existing = await db.QuickRatings
                    .FirstOrDefaultAsync(r => r.LocalBeerId == dto.BeerId && (r.UserId == uid || r.Fingerprint == uid));

                double totalSum = beer.Rating * Math.Max(beer.RatingCount, 0);

                if (existing is null)
                {
                    db.QuickRatings.Add(new QuickRating
                    {
                        LocalBeerId = dto.BeerId,
                        UserId = uid,
                        Fingerprint = uid,
                        IpHash = (ctx.Connection.RemoteIpAddress?.ToString() is string ip)
                                    ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip)))
                                    : null,
                        Score = dto.Score,
                        CreatedAt = DateTime.UtcNow
                    });

                    var newCount = beer.RatingCount + 1;
                    var newAvg = (totalSum + dto.Score) / Math.Max(newCount, 1);
                    beer.RatingCount = newCount;
                    beer.Rating = Math.Round(newAvg, 2);
                }
                else
                {
                    totalSum = totalSum - existing.Score + dto.Score;
                    var newAvg = totalSum / Math.Max(beer.RatingCount, 1);
                    beer.Rating = Math.Round(newAvg, 2);

                    existing.Score = dto.Score;
                    existing.UserId = uid;
                    existing.Fingerprint = uid;
                    existing.CreatedAt = DateTime.UtcNow;
                    db.QuickRatings.Update(existing);
                }

                await db.SaveChangesAsync();

                var ua = ctx.Request.Headers.UserAgent.ToString();
                var ipHash2 = (ctx.Connection.RemoteIpAddress?.ToString() is string ip2)
                                ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip2)))
                                : null;
                await logger.LogAsync(uid, "rating.set", "LocalBeer", dto.BeerId.ToString(), $"score {dto.Score}", null, ipHash2, ua);

                return Results.Ok(new { avg = beer.Rating, count = beer.RatingCount });
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

            // GET /api/ratings/quick/mine?beerId=123
            api.MapGet("/ratings/quick/mine", async (
                int beerId,
                AppDbContext db,
                HttpContext ctx,
                UserManager<ApplicationUser> userManager) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var uid = userManager.GetUserId(ctx.User);
                if (string.IsNullOrWhiteSpace(uid)) return Results.Unauthorized();

                var score = await db.QuickRatings
                    .AsNoTracking()
                    .Where(r => r.LocalBeerId == beerId && (r.UserId == uid || r.Fingerprint == uid))
                    .Select(r => (int?)r.Score)
                    .FirstOrDefaultAsync();

                return Results.Ok(new { score });
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

            return api;
        }
    }
}
