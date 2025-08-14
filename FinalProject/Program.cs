using FinalProject.Setup;
using FinalProject.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddAppCoreServices(builder.Configuration);
builder.Services.AddIdentityAndCookies();

// Build
var app = builder.Build();

// Pipeline
app.UseAppPipeline();

// API Endpoints
app.MapCommunityEndpoints();   // Ratings, Comments, Favorites, Me
app.MapFlavorMatchEndpoints(); // Flavor Match (แนะนำจากรส)

// Pages
app.MapRazorPages();

app.Run();
