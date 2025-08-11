using MudBlazor.Services;
using Stockly.Components;
using Stockly.Services;


var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add custom services
builder.Services.AddScoped<FirebaseService>();
builder.Services.AddScoped<UserStateService>();
builder.Services.AddScoped<DrawerService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<SecureStorageService>();

// Add health checks
builder.Services.AddHealthChecks();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

<<<<<<< Updated upstream
app.Run();
=======
// Add health check endpoint
app.MapHealthChecks("/health");

// Configure the app to listen on the PORT environment variable for Firebase Cloud Run
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"Starting application on port: {port}");
app.Run($"http://0.0.0.0:{port}");
>>>>>>> Stashed changes
