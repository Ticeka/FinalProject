// FinalProject/Setup/IdentitySeeder.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FinalProject.Models;

namespace FinalProject.Setup
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1) สร้าง Role ที่ต้องการ
            var roles = new[] { "Admin", "User" };
            foreach (var r in roles)
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));

            // 2) สร้างแอดมินเริ่มต้น (แก้อีเมล/รหัสผ่านตามต้องการ)
            var adminEmail = "admin@local.test";
            var adminPassword = "Admin123!"; // ควรเปลี่ยนเป็นรหัสจริงที่ปลอดภัย
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var create = await userManager.CreateAsync(admin, adminPassword);
                if (!create.Succeeded)
                    throw new Exception("Create admin failed: " +
                        string.Join(", ", create.Errors.Select(e => e.Description)));
            }

            // 3) ใส่ Role ให้แอดมิน
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
