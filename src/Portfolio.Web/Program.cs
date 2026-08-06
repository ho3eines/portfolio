using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Portfolio.Web;
using Portfolio.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Resolve API base address from configuration/environment; fall back to API launch URL.
var apiBase = builder.Configuration["ApiBaseUrl"]
             ?? builder.Configuration["Api:BaseUrl"]
             ?? builder.HostEnvironment.BaseAddress;
// Ensure the address is an absolute URI (BaseAddress requires it).
if (!apiBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
    !apiBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    apiBase = apiBase.StartsWith('/') ? $"https://localhost:49325{apiBase}" : $"https://localhost:49325/{apiBase}";
}
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<ApiAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<TranslationService>();

var host = builder.Build();
var t = host.Services.GetRequiredService<TranslationService>();
await t.InitAsync();
await host.RunAsync();
