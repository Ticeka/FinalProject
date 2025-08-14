using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinalProject.Data;
using FinalProject.Services;

namespace FinalProject.Setup
{
    public static class AppCoreServices
    {
        public static IServiceCollection AddAppCoreServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            services.AddRazorPages()
                .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

            services.ConfigureHttpJsonOptions(o =>
                o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddSingleton<IEmailSender, NullEmailSender>();
            return services;
        }
    }
}
