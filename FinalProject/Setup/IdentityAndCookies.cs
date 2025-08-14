using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FinalProject.Data;
using FinalProject.Models;

namespace FinalProject.Setup
{
    public static class IdentityAndCookies
    {
        public static IServiceCollection AddIdentityAndCookies(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
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

            services.ConfigureApplicationCookie(opt =>
            {
                opt.LoginPath = "/Identity/Account/Login";
                opt.LogoutPath = "/Identity/Account/Logout";
                opt.AccessDeniedPath = "/Identity/Account/AccessDenied";
                opt.SlidingExpiration = true;
                opt.ExpireTimeSpan = TimeSpan.FromDays(14);
                opt.Cookie.Name = ".FinalProject.Auth";
                opt.Cookie.Path = "/";
                opt.Cookie.HttpOnly = true;
                opt.Cookie.IsEssential = true;
                opt.Cookie.SameSite = SameSiteMode.Lax;
                opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;

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

            return services;
        }
    }
}
