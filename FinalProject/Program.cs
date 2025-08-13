using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;          // For endpoint extensions (Produces, etc.)
using Microsoft.AspNetCore.Http;             // For StatusCodes / endpoint metadata
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;      // ApplicationUser, BeerComment, LocalBeer, ...
using FinalProject.Services;   // NullEmailSender
using System.Linq;

// ---------------------------
// Configure Services
// ---------------------------
var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.Password.RequireDigit = false;
        opts.Password.RequireNonAlphanumeric = false;
        opts.Password.RequireUppercase = false;
        opts.Password.RequiredLength = 6;

        opts.SignIn.RequireConfirmedAccount = false;
        opts.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Email (mock)
builder.Services.AddSingleton<IEmailSender, NullEmailSender>();

// Cookies
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Identity/Account/Login";
    opt.LogoutPath = "/Identity/Account/Logout";
    opt.AccessDeniedPath = "/Identity/Account/AccessDenied";
    opt.SlidingExpiration = true;
    opt.ExpireTimeSpan = TimeSpan.FromDays(14);
    opt.Cookie.IsEssential = true;
});

// Razor Pages + JSON (camelCase)
builder.Services.AddRazorPages()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Dev helper for EF
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// ---------------------------
// Pipeline
// ---------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages(); // รวมหน้า Identity ด้วย

// =====================================================
// Quick Ratings API — 1 อุปกรณ์/1 เบียร์ โหวตได้ครั้งเดียว
// =====================================================
const string FP_COOKIE = "qr_fp";

app.MapPost("/api/ratings/quick", async (
    [FromBody] QuickRateDto dto,
    AppDbContext db,
    HttpContext ctx) =>
{
    if (dto is null) return Results.BadRequest("payload is required");
    if (dto.Score < 1 || dto.Score > 5) return Results.BadRequest("score must be 1..5");

    var beer = await db.LocalBeers.FindAsync(dto.BeerId);
    if (beer is null) return Results.NotFound();

    // อุปกรณ์จากคุกกี้ (กันเคลียร์ localStorage)
    string deviceId;
    if (ctx.Request.Cookies.TryGetValue(FP_COOKIE, out var cookieFp) && !string.IsNullOrWhiteSpace(cookieFp))
    {
        deviceId = cookieFp;
    }
    else
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

    var fingerprint = deviceId;

    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var ipHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip)));

    // เคยโหวตแล้ว?
    var exists = await db.QuickRatings
        .AsNoTracking()
        .AnyAsync(r => r.LocalBeerId == dto.BeerId && r.Fingerprint == fingerprint);

    if (exists) return Results.Conflict("already rated by this device");

    // บันทึกโหวตใหม่
    db.QuickRatings.Add(new QuickRating
    {
        LocalBeerId = dto.BeerId,
        Score = dto.Score,
        IpHash = ipHash,
        Fingerprint = fingerprint,
        CreatedAt = DateTime.UtcNow
    });

    // อัปเดตค่าเฉลี่ยแบบ O(1)
    var total = (beer.Rating * beer.RatingCount) + dto.Score;
    beer.RatingCount += 1;
    beer.Rating = Math.Round(total / Math.Max(beer.RatingCount, 1), 2);

    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        // ถ้าตั้ง Unique Index (LocalBeerId, Fingerprint) ไว้
        return Results.Conflict("already rated by this device");
    }

    return Results.Ok(new { avg = beer.Rating, count = beer.RatingCount });
})
.AllowAnonymous()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status409Conflict);

// =======================================
// Comments API — GET / POST / DELETE
// =======================================

// Helper: สร้าง ipHash
static string GetIpHash(HttpContext ctx)
{
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip)));
}

// GET: /api/beers/{id}/comments?skip=0&take=20
app.MapGet("/api/beers/{id:int}/comments", async (
    int id, int skip, int take,
    AppDbContext db,
    HttpContext ctx,
    UserManager<ApplicationUser> userManager) =>
{
    skip = Math.Max(0, skip);
    take = Math.Clamp(take <= 0 ? 20 : take, 1, 100);

    var uid = (ctx.User?.Identity?.IsAuthenticated ?? false) ? userManager.GetUserId(ctx.User) : null;
    var ipHash = GetIpHash(ctx);
    var now = DateTime.UtcNow;
    var guestWindow = TimeSpan.FromDays(1);

    var raw = await db.BeerComments
        .AsNoTracking()
        .Where(c => c.LocalBeerId == id)
        .OrderByDescending(c => c.CreatedAt)
        .Skip(skip).Take(take)
        .Select(c => new
        {
            c.Id,
            c.Body,
            c.DisplayName,
            c.UserId,
            c.UserName,
            c.CreatedAt,
            c.IpHash
        })
        .ToListAsync();

    var items = raw.Select(c => new CommentView(
        c.Id,
        c.Body,
        string.IsNullOrWhiteSpace(c.UserName) ? (c.DisplayName ?? "Guest") : c.UserName!,
        c.CreatedAt,
        // CanDelete เงื่อนไข: เจ้าของผู้ใช้ หรือ guest ที่ IP เดิมภายใน 24 ชม.
        (c.UserId != null && c.UserId == uid) ||
        (c.UserId == null && string.Equals(c.IpHash, ipHash, StringComparison.OrdinalIgnoreCase) && (now - c.CreatedAt) <= guestWindow)
    )).ToList();

    return Results.Ok(items);
})
.AllowAnonymous()
.Produces(StatusCodes.Status200OK);

// POST: /api/beers/{id}/comments
app.MapPost("/api/beers/{id:int}/comments", async (
    int id,
    [FromBody] NewCommentDto dto,
    AppDbContext db,
    HttpContext ctx,
    UserManager<ApplicationUser> userManager) =>
{
    if (dto is null || string.IsNullOrWhiteSpace(dto.Body))
        return Results.BadRequest("comment is required");
    if (dto.Body.Length > 1000)
        return Results.BadRequest("comment too long");

    var beer = await db.LocalBeers.FindAsync(id);
    if (beer is null) return Results.NotFound("beer not found");

    string? uid = null, uname = null, display = dto.DisplayName?.Trim();

    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        uid = userManager.GetUserId(ctx.User);
        // ถ้า Name ว่าง ลองดึงจาก store
        uname = ctx.User.Identity!.Name ?? (await userManager.GetUserNameAsync(await userManager.GetUserAsync(ctx.User)));
    }
    else
    {
        display = string.IsNullOrWhiteSpace(display) ? "Guest" : display;
        if (!string.IsNullOrEmpty(display) && display.Length > 100) display = display[..100];
    }

    var ipHash = GetIpHash(ctx);

    var cmt = new BeerComment
    {
        LocalBeerId = id,
        Body = dto.Body.Trim(),
        DisplayName = uid is null ? display : null,
        UserId = uid,
        UserName = uname,
        IpHash = ipHash,
        CreatedAt = DateTime.UtcNow
    };

    try
    {
        db.BeerComments.Add(cmt);
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        return Results.Problem("cannot save comment", statusCode: 500);
    }

    // ผู้โพสต์พึ่งโพสต์เอง ให้ CanDelete = true
    var view = new CommentView(
        cmt.Id,
        cmt.Body,
        string.IsNullOrWhiteSpace(cmt.UserName) ? (cmt.DisplayName ?? "Guest") : cmt.UserName!,
        cmt.CreatedAt,
        true
    );

    return Results.Created($"/api/beers/{id}/comments/{cmt.Id}", view);
})
.Produces<CommentView>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError)
.AllowAnonymous();

// DELETE: /api/beers/{beerId}/comments/{commentId}
app.MapDelete("/api/beers/{beerId:int}/comments/{commentId:int}", async (
    int beerId, int commentId,
    AppDbContext db,
    HttpContext ctx,
    UserManager<ApplicationUser> userManager) =>
{
    var cmt = await db.BeerComments
        .FirstOrDefaultAsync(c => c.Id == commentId && c.LocalBeerId == beerId);

    if (cmt is null) return Results.NotFound();

    var isAuth = ctx.User?.Identity?.IsAuthenticated == true;
    var uid = isAuth ? userManager.GetUserId(ctx.User) : null;

    // แอดมินลบได้ทุกอัน (ถ้าคุณมี role Admin)
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
            // guest: ต้องมาจาก IP เดิม และภายใน 24 ชม.
            var ipHash = GetIpHash(ctx);
            if (!string.Equals(cmt.IpHash, ipHash, StringComparison.OrdinalIgnoreCase))
                return Results.Forbid();

            if ((DateTime.UtcNow - cmt.CreatedAt) > TimeSpan.FromDays(1))
                return Results.Forbid();
        }
    }

    db.BeerComments.Remove(cmt);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status404NotFound);

// GET: /api/beers/{id}/favorite  -> { isFavorite: bool }
app.MapGet("/api/beers/{id:int}/favorite", async (
    int id,
    AppDbContext db,
    HttpContext ctx,
    UserManager<ApplicationUser> userManager
) =>
{
    if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var uid = userManager.GetUserId(ctx.User);
    var isFav = await db.BeerFavorites.AsNoTracking()
        .AnyAsync(f => f.LocalBeerId == id && f.UserId == uid);
    return Results.Ok(new { isFavorite = isFav });
})
.RequireAuthorization()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);

// POST: /api/beers/{id}/favorite  -> add (idempotent)
app.MapPost("/api/beers/{id:int}/favorite", async (
    int id,
    AppDbContext db,
    HttpContext ctx,
    UserManager<ApplicationUser> userManager
) =>
{
    if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var uid = userManager.GetUserId(ctx.User);

    var beer = await db.LocalBeers.FindAsync(id);
    if (beer is null) return Results.NotFound("beer not found");

    var exists = await db.BeerFavorites
        .AnyAsync(f => f.LocalBeerId == id && f.UserId == uid);
    if (exists) return Results.Ok(new { ok = true }); // idempotent

    db.BeerFavorites.Add(new BeerFavorite { UserId = uid!, LocalBeerId = id });

    // อัปเดตสถิติ
    var stats = await db.UserStats.FindAsync(uid);
    if (stats == null)
    {
        stats = new UserStats { UserId = uid!, Reviews = 0, Favorites = 0, Badges = 0 };
        db.UserStats.Add(stats);
    }
    stats.Favorites = Math.Max(0, stats.Favorites + 1);

    await db.SaveChangesAsync();
    return Results.Ok(new { ok = true });
})
.RequireAuthorization()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);

// DELETE: /api/beers/{id}/favorite  -> remove (idempotent)
app.MapDelete("/api/beers/{id:int}/favorite", async (
    int id,
    AppDbContext db,
    HttpContext ctx,
    UserManager<ApplicationUser> userManager
) =>
{
    if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var uid = userManager.GetUserId(ctx.User);

    var fav = await db.BeerFavorites
        .FirstOrDefaultAsync(f => f.LocalBeerId == id && f.UserId == uid);
    if (fav != null) db.BeerFavorites.Remove(fav);

    // อัปเดตสถิติ
    var stats = await db.UserStats.FindAsync(uid);
    if (stats != null) stats.Favorites = Math.Max(0, stats.Favorites - 1);

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status401Unauthorized);

// GET: /api/me/favorites  -> รายการเบียร์ที่ถูกใจของฉัน
app.MapGet("/api/me/favorites", async (
    AppDbContext db,
    HttpContext ctx,
    UserManager<ApplicationUser> userManager
) =>
{
    if (!(ctx.User?.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
    var uid = userManager.GetUserId(ctx.User);

    var items = await db.BeerFavorites
        .AsNoTracking()
        .Where(f => f.UserId == uid)
        .OrderByDescending(f => f.CreatedAt)
        .Join(db.LocalBeers,
              f => f.LocalBeerId,
              b => b.Id,
              (f, b) => new {
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

app.Run();

// ---------------------------------
// DTO / View Models (สำหรับ API)
// ---------------------------------
public record QuickRateDto(int BeerId, int Score);

// ส่งชื่อที่แสดงมาได้ในกรณี guest
public record NewCommentDto(string? DisplayName, string Body);

// ใช้ให้ฝั่ง JS render + คุมปุ่มลบ
public record CommentView(int Id, string Body, string Author, DateTime CreatedAt, bool CanDelete);
