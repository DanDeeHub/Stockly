using MudBlazor.Services;
using Stockly.Components;
using Stockly.Interface;
using Stockly.Services;


var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add custom services
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<FirebaseService>();
builder.Services.AddScoped<UserStateService>();
builder.Services.AddScoped<DrawerService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<SecureStorageService>();
builder.Services.AddHttpClient("StocklyAPI", client => 
{
    client.BaseAddress = new Uri("http://192.168.1.144:5103/");
});
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

await app.RunAsync();
