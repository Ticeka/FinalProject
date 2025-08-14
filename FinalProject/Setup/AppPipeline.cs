using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace FinalProject.Setup
{
    public static class AppPipeline
    {
        public static WebApplication UseAppPipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();
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

            return app;
        }
    }
}
