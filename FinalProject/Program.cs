// Program.cs
using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using FinalProject.Models;
using FinalProject.Endpoints;
using FinalProject.Setup;     // มี IdentitySeeder
using FinalProject.Services; // <= เพิ่ม (ให้ตรง namespace ของ IActivityLogger/ActivityLogger)

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddAppCoreServices(builder.Configuration);
builder.Services.AddIdentityAndCookies();

// ลงทะเบียนตัวเขียน Log (จำเป็นต่อการใช้งาน ActivityLogger)
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();

// นโยบาย (ตามเดิม)
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

var app = builder.Build();

// Seed roles + admin เริ่มต้น (ถ้ามีใน seeder)
await IdentitySeeder.SeedAsync(app.Services);

// Pipeline (รวม StaticFiles/HTTPS/Route/Auth/ฯลฯ ตามที่คุณจัดใน UseAppPipeline)
app.UseAppPipeline();

// API Endpoints (ตามเดิม)
app.MapCommunityEndpoints();
app.MapFlavorMatchEndpoints();

// Razor Pages (หน้า /Admin/Logs จะมากับไฟล์ที่ผมให้)
app.MapRazorPages();

app.Run();
