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
// Browser code must never call a developer's localhost. By default all API
// requests stay on the current origin and a reverse proxy forwards /api to the
// ASP.NET API. This works in deployed sites and hosted previews alike.
//
// ApiBaseUrl / Api:BaseUrl is only an explicit opt-in for a separate API host
// (for example a local development machine), and must be an absolute URL.
var configuredApiBase = builder.Configuration["ApiBaseUrl"]
    ?? builder.Configuration["Api:BaseUrl"];

var apiBase = string.IsNullOrWhiteSpace(configuredApiBase)
    ? builder.HostEnvironment.BaseAddress
    : configuredApiBase;

if (!Uri.TryCreate(apiBase, UriKind.Absolute, out var apiUri))
    throw new InvalidOperationException("ApiBaseUrl must be an absolute URL, e.g. https://api.example.com/.");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiUri });

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
