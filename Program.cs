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

// Remove HTTPS redirection for Firebase Cloud Run
// app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add health check endpoint
app.MapHealthChecks("/health");

await app.RunAsync();
