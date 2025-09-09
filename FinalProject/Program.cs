// Program.cs
using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using FinalProject.Models;
using FinalProject.Endpoints;
using FinalProject.Setup; // มี IdentitySeeder

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddAppCoreServices(builder.Configuration);
builder.Services.AddIdentityAndCookies();

// เพิ่มนโยบาย (ทางเลือก แต่แนะนำ)
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

var app = builder.Build();

// เรียก seed roles + admin เริ่มต้น (ถ้ามีใน seeder)
await IdentitySeeder.SeedAsync(app.Services);



// Pipeline
app.UseAppPipeline();

// API Endpoints
app.MapCommunityEndpoints();
app.MapFlavorMatchEndpoints();

// Pages
app.MapRazorPages();

app.Run();
