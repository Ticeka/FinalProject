using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Json;   // ← เพิ่มบรรทัดนี้
using FinalProject.Data;
using FinalProject.Models;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Razor Pages (ตัวเลือก camelCase สำหรับ MVC/RP)
builder.Services.AddRazorPages()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ให้ Minimal API เป็น camelCase ด้วย
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

// Endpoint ให้ดาวแบบไม่ล็อกอิน
app.MapPost("/api/ratings/quick", async (
    [FromBody] QuickRateDto dto,
    AppDbContext db,
    HttpContext ctx) =>
{
    if (dto.Score < 1 || dto.Score > 5)
        return Results.BadRequest("score must be 1..5");

    var beer = await db.LocalBeers.FindAsync(dto.BeerId);
    if (beer == null) return Results.NotFound();

    // แฮช IP (ไม่เก็บข้อมูลส่วนบุคคลตรง ๆ)
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var ipHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip)));

    // บันทึกประวัติการโหวต
    db.QuickRatings.Add(new QuickRating
    {
        LocalBeerId = dto.BeerId,
        Score = dto.Score,
        IpHash = ipHash,
        Fingerprint = dto.Fingerprint,
        CreatedAt = DateTime.UtcNow
    });

    // อัปเดตค่าเฉลี่ยแบบ O(1)
    // หมายเหตุ: ถ้าในโมเดลของคุณ Rating/RatingCount เป็น non-nullable อยู่แล้ว ใช้แบบนี้ได้เลย
    var total = (beer.Rating * beer.RatingCount) + dto.Score;
    beer.RatingCount += 1;
    beer.Rating = Math.Round(total / beer.RatingCount, 2);

    await db.SaveChangesAsync();

    return Results.Ok(new { avg = beer.Rating, count = beer.RatingCount });
});

app.Run();
