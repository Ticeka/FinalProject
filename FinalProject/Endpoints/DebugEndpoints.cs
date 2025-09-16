namespace FinalProject.Endpoints
{
    public static class DebugEndpoints
    {
        public static IEndpointRouteBuilder MapDebugEndpoints(this IEndpointRouteBuilder api)
        {
            api.MapGet("/debug/whoami", (HttpContext ctx) =>
            {
                var u = ctx.User;
                return Results.Ok(new
                {
                    isAuth = u?.Identity?.IsAuthenticated ?? false,
                    name = u?.Identity?.Name,
                    claims = u?.Claims.Select(c => new { c.Type, c.Value })
                });
            });

            return api;
        }
    }
}
