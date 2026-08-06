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
// Priority: explicit config → environment variable → same-origin (production)
//           → localhost dev fallback
var apiBase = builder.Configuration["ApiBaseUrl"]
    ?? builder.Configuration["Api:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("PORTFOLIO_API_URL")
    ?? builder.HostEnvironment.BaseAddress;

// If running on the same host as the API (production / reverse-proxy), use
// the app's own BaseAddress.  In local dev the API runs on a different port,
// so fall back to the well-known dev address.
if (apiBase == builder.HostEnvironment.BaseAddress
    && !builder.HostEnvironment.IsDevelopment())
{
    // Same-origin in production — BaseAddress is correct.
}
else if (builder.HostEnvironment.IsDevelopment()
         && !apiBase.Contains("localhost", StringComparison.OrdinalIgnoreCase))
{
    // Dev mode: API is on port 49325 unless overridden.
    apiBase = "https://localhost:49325";
}

// Ensure absolute URI.
if (!apiBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
    !apiBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    apiBase = $"https://localhost:49325/{apiBase.TrimStart('/')}";
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

await host.RunAsync();
