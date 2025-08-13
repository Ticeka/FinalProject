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
using FinalProject.Models;      // ApplicationUser, BeerComment, LocalBeer, ... (และใช้ DTO จากที่นี่)
using FinalProject.Services;   // NullEmailSender
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;

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

// Cookies (สำคัญ)
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Identity/Account/Login";
    opt.LogoutPath = "/Identity/Account/Logout";
    opt.AccessDeniedPath = "/Identity/Account/AccessDenied";
    opt.SlidingExpiration = true;
    opt.ExpireTimeSpan = TimeSpan.FromDays(14);

    // ตั้งค่าคุกกี้ให้แน่น
    opt.Cookie.Name = ".FinalProject.Auth";
    opt.Cookie.Path = "/";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
    opt.Cookie.SameSite = SameSiteMode.Lax;
    // ถ้าทดสอบบน http ให้เปลี่ยนเป็น CookieSecurePolicy.None ชั่วคราว
    opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    // ให้ /api/* ได้ 401/403 แทน 302 redirect
    opt.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        }
    };
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

    string? meId = (ctx.User?.Identity?.IsAuthenticated ?? false)
        ? userManager.GetUserId(ctx.User)
        : null;

    static string MaskEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? "User" : email.Split('@')[0];

    // ดึงคอมเมนต์ชุดหลัก
    var raw = await db.BeerComments
        .AsNoTracking()
        .Where(c => !c.IsDeleted && c.LocalBeerId == id)
        .OrderByDescending(c => c.CreatedAt)
        .Skip(skip).Take(take)
        .Select(c => new
        {
            c.Id,
            c.Body,
            c.DisplayName,   // ของ guest
            c.UserId,        // เอาไว้ map ผู้ใช้จริง
            c.UserName,
            c.CreatedAt
            // ถ้าคุณมีฟิลด์ c.UserRating ใน BeerComment ให้ select มาด้วย
        })
        .ToListAsync();

    // ดึงข้อมูลผู้ใช้ของทุกคอมเมนต์ทีเดียว
    var userIds = raw.Where(r => r.UserId != null).Select(r => r.UserId!).Distinct().ToList();
    var userMap = await db.Users
    .AsNoTracking()
    .Where(u => userIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id); // value type = ApplicationUser

    // 2) ใช้งาน
    var items = raw.Select(r =>
    {
        FinalProject.Models.ApplicationUser? u = null;
        if (r.UserId != null)
            userMap.TryGetValue(r.UserId!, out u);

        static string MaskEmail(string? email) => string.IsNullOrWhiteSpace(email) ? "User" : email.Split('@')[0];

        var disp = (u != null)
            ? (u.DisplayName ?? u.UserName ?? MaskEmail(u.Email))
            : (r.DisplayName ?? "Guest");

        var avatarUrl = u?.AvatarUrl;
        var canDelete = (meId != null && r.UserId == meId);
        int? rating = null;

        return new CommentView(
            Id: r.Id,
            Body: r.Body,
            Author: disp,
            CreatedAt: r.CreatedAt,
            CanDelete: canDelete,
            DisplayName: disp,
            AvatarUrl: avatarUrl,
            Rating: rating
        );
    }).ToList();

    return Results.Ok(items);
})
.AllowAnonymous()
.Produces(StatusCodes.Status200OK);

// POST: /api/beers/{id}/comments
app.MapPost("/api/beers/{id:int}/comments", async (
    int id,
    [FromBody] NewCommentDto dto, // หรือ CommentCreateDto ก็ได้ (ทั้งคู่อยู่ใน FinalProject.Models แล้ว)
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
    ApplicationUser? me = null;

    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        me = await userManager.GetUserAsync(ctx.User);
        uid = me?.Id;
        uname = me?.UserName;
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
        CreatedAt = DateTime.UtcNow
        // ถ้าคุณมี BeerComment.UserRating และอยากบันทึก:
        // UserRating = dto.UserRating is >=1 and <=5 ? dto.UserRating : null
    };

    db.BeerComments.Add(cmt);
    await db.SaveChangesAsync();

    // สร้าง response พร้อมชื่อ/รูป
    static string MaskEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? "User" : email.Split('@')[0];

    var disp = me != null
        ? (me.DisplayName ?? me.UserName ?? MaskEmail(me.Email))
        : (cmt.DisplayName ?? "Guest");

    var avatarUrl = me?.AvatarUrl;
    int? rating = null; // ถ้าบันทึกคะแนนต่อคอมเมนต์ ใช้ dto.UserRating แทน

    var view = new CommentView(
        Id: cmt.Id,
        Body: cmt.Body,
        Author: disp,
        CreatedAt: cmt.CreatedAt,
        CanDelete: true,
        DisplayName: disp,
        AvatarUrl: avatarUrl,
        Rating: rating
    );

    return Results.Created($"/api/beers/{id}/comments/{cmt.Id}", view);
})
.AllowAnonymous()
.Produces<CommentView>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);


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

// ==============================
// Favorites API (ต้องล็อกอิน)
// ==============================

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

// Debug: เช็คว่า API เห็น Auth ไหม
app.MapGet("/api/debug/whoami", (HttpContext ctx) =>
{
    var u = ctx.User;
    return Results.Ok(new
    {
        isAuth = u?.Identity?.IsAuthenticated ?? false,
        name = u?.Identity?.Name,
        claims = u?.Claims.Select(c => new { c.Type, c.Value })
    });
});

app.Run();
