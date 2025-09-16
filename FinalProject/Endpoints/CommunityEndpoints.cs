using Microsoft.AspNetCore.Routing;

namespace FinalProject.Endpoints
{
    public static class CommunityEndpoints
    {
        public static IEndpointRouteBuilder MapCommunityEndpoints(this IEndpointRouteBuilder app)
        {
            
            var g = app.MapGroup("/api");

            
            g.MapRatingsEndpoints();
            g.MapCommentsEndpoints();
            g.MapFavoritesEndpoints();
            g.MapDebugEndpoints();

            return app;
        }
    }
}
