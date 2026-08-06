using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Portfolio.Web;
using Portfolio.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── Resolve API base address ──────────────────────────────────────────────
// Priority: explicit config (appsettings.Development.json / env var) →
//           PORTFOLIO_API_URL env var → sensible default.
// In Development the API runs on a separate port (https://localhost:49325).
// In production the API is served same-origin through the reverse proxy, so
// the app's own BaseAddress is used — no hardcoded localhost shipped to users.
var apiBase = builder.Configuration["ApiBaseUrl"]
    ?? builder.Configuration["Api:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("PORTFOLIO_API_URL");

if (string.IsNullOrEmpty(apiBase))
{
    apiBase = builder.HostEnvironment.IsDevelopment()
        ? "https://localhost:49325"
        : builder.HostEnvironment.BaseAddress;
}

// Ensure absolute URI.
if (!apiBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
    !apiBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    apiBase = $"https://{apiBase.TrimStart('/')}";
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<ApiAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<TranslationService>();

var host = builder.Build();

// Pre-load translations before the first render so LanguageGate works immediately.
var t = host.Services.GetRequiredService<TranslationService>();
await t.InitAsync();

// Restore a persisted JWT before the first render, so refreshing /admin
// (or any authorized page) does not bounce an already signed-in user to
// the login page while the auth state is still anonymous.
await host.Services.GetRequiredService<AuthService>().InitializeAsync();

await host.RunAsync();
