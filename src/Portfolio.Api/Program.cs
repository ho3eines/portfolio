using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Services;
using Portfolio.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ErrorLogService>();

// Dapper — SqlConnectionFactory instead of DbContext
// NOTE: In production, override via env var ConnectionStrings__PortfolioDb.
var connStr = builder.Configuration.GetConnectionString("PortfolioDb")
    ?? "workstation id=support;password=123456;packet size=4096;user id=sa;data source=.;persist security info=false;initial catalog=PortfolioDB;Encrypt=False";
builder.Services.AddSingleton(new SqlConnectionFactory(connStr));

// Auto-Start Resource Runner
builder.Services.AddHostedService<ResourceAutoStartService>();

// JWT — the signing key must NOT be committed for production.
// Development key comes from appsettings.Development.json; in production set
// the Jwt__Key environment variable (or user-secrets). Startup fails fast
// with a clear message if it is missing.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "Jwt:Key is not configured. In Development it is provided by " +
        "appsettings.Development.json; in production set the Jwt__Key environment variable.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => {
    o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PortfolioApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PortfolioClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new() { Description = "JWT: Bearer {token}", Name = "Authorization", In = Microsoft.OpenApi.Models.ParameterLocation.Header, Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey });
    c.AddSecurityRequirement(new() { { new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
});
builder.Services.AddCors(o => o.AddPolicy("All", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("All");
app.UseMiddleware<Portfolio.Api.Middleware.ErrorHandlingMiddleware>();
app.UseAuthentication(); app.UseAuthorization();

// ── Static assets: only serve real files, never a broken Blazor shell ─────────
// The Blazor WASM framework (_framework/*) is emitted by `dotnet build` /
// `dotnet publish` into the publish output, NOT into source wwwroot. Serving
// source wwwroot's index.html on the API port would cause the browser to
// request `/_framework/blazor.webassembly.js` from the API host → 404:
//   GET https://localhost:49325/_framework/blazor.webassembly.js 404
// The portfolio UI is served by Portfolio.Web (https://localhost:49323), not
// the API (https://localhost:49325). This block therefore:
//   • prefers the publish output (which already contains _framework) if present,
//   • otherwise serves only existing static files (lang/, css/, js/, images/ …),
//   • intercepts /_framework/* with a diagnostic when no publish output exists,
//   • and returns a helpful HTML page for GET / on the API host instead of the
//     Blazor index.html that would immediately 404 on _framework.
var publishedWwwRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
var sourceWwwRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "Portfolio.Web", "wwwroot"));

string? effectiveWwwRoot = null;
bool hasPublishedFramework = Directory.Exists(Path.Combine(publishedWwwRoot, "_framework"));
if (hasPublishedFramework)
    effectiveWwwRoot = publishedWwwRoot;
else if (Directory.Exists(sourceWwwRoot))
    effectiveWwwRoot = sourceWwwRoot;

if (effectiveWwwRoot is not null)
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(effectiveWwwRoot);

    if (hasPublishedFramework)
    {
        // Publish output already contains _framework + index.html → full SPA hosting
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
    }
    else
    {
        // Source wwwroot fallback — serve only files that physically exist
        // (lang/*.json, css/*, js/*, images/*, manifest.json, etc.). Do NOT
        // use UseDefaultFiles so GET / does not return index.html which would
        // then 404 on _framework. The stub at wwwroot/_framework/blazor.webassembly.js
        // now renders a diagnostic/static preview, but we also handle the API-root.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            // Don't serve unknown types; keep 404s explicit for debugging
            ServeUnknownFileTypes = false
        });

        // Diagnostic for direct _framework hits on the API host when no publish
        // output exists. The file now exists as a stub (static preview), so this
        // handler is a safety net for any other missing framework files.
        app.MapGet("/_framework/{*path}", (string path) =>
            Results.Content(
                $"/* Portfolio diagnostic: GET /_framework/{path} on the API host — no publish output. */\n" +
                $"console.error('[Portfolio] GET /_framework/{path} → API host has no published Blazor framework. '\n" +
                $"  + 'The portfolio UI is at https://localhost:49323 (Portfolio.Web), not https://localhost:49325 (Portfolio.Api). '\n" +
                $"  + 'Run both: `cd src/Portfolio.Api && dotnet run` and `cd src/Portfolio.Web && dotnet run`.');\n",
                "application/javascript", statusCode: 404));

        // Friendly landing for GET / on the API host — explains the two servers
        app.MapGet("/", () => Results.Content(
            "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">"
            + "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
            + "<title>Portfolio API — running</title>"
            + "<style>body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;background:#080e19;color:#f5f7fb;font-family:Manrope,system-ui,sans-serif;padding:24px}"
            + ".card{max-width:720px;width:100%;background:#111c2e;border:1px solid rgba(224,235,250,.10);border-radius:24px;padding:28px;box-shadow:0 24px 70px rgba(0,0,0,.30)}"
            + "h1{font-family:DM Serif Display,Georgia,serif;font-weight:400;margin:0 0 10px}"
            + "code{background:rgba(255,255,255,.06);padding:2px 6px;border-radius:6px;font-size:13px}"
            + "a{color:#73ddcf;text-decoration:none}a:hover{text-decoration:underline}"
            + ".btn{display:inline-flex;align-items:center;gap:8px;background:#f5f7fb;color:#080e19;padding:10px 18px;border-radius:999px;text-decoration:none;font-weight:700}"
            + ".btn2{display:inline-flex;align-items:center;gap:8px;background:transparent;color:#f5f7fb;border:1px solid rgba(224,235,250,.18);padding:10px 18px;border-radius:999px;text-decoration:none}"
            + "</style></head><body><div class=\"card\">"
            + "<div style=\"font-size:11px;letter-spacing:.18em;text-transform:uppercase;color:#f0b45e;margin-bottom:10px\">Portfolio API &middot; running</div>"
            + "<h1>API is up — the portfolio UI is on port 49323</h1>"
            + "<p style=\"color:#9aaac0;line-height:1.6\">You opened <code>https://localhost:49325</code> (the <strong style=\"color:#f5f7fb\">API</strong>). The portfolio UI is served by <strong style=\"color:#f5f7fb\">Portfolio.Web</strong> at <code>https://localhost:49323</code> (or <code>http://localhost:49324</code>).</p>"
            + "<p style=\"display:flex;gap:10px;flex-wrap:wrap;margin:18px 0\">"
            + "<a class=\"btn\" href=\"https://localhost:49323\">Open portfolio &rarr;</a>"
            + "<a class=\"btn2\" href=\"http://localhost:49324\">http://localhost:49324</a>"
            + "<a class=\"btn2\" href=\"/swagger\">Swagger</a></p>"
            + "<div style=\"background:rgba(240,180,94,.08);border:1px solid rgba(240,180,94,.18);border-radius:12px;padding:12px 14px\">"
            + "<div style=\"font-weight:700;color:#f0b45e;margin-bottom:6px\">Run both servers</div>"
            + "<code style=\"display:block;white-space:pre-wrap;line-height:1.6\"># Terminal 1 — API (49325)\ncd src/Portfolio.Api && dotnet run\n\n# Terminal 2 — Web (49323)\ncd src/Portfolio.Web && dotnet run</code></div>"
            + "<p style=\"margin:14px 0 0;font-size:13px;color:#9aaac0\">Static preview without dotnet: <code>npx serve src/Portfolio.Web/wwwroot -l 5173</code> &mdash; the stub at <code>/_framework/blazor.webassembly.js</code> now renders a static portfolio instead of 404.</p>"
            + "<p style=\"margin:10px 0 0;display:flex;gap:12px;flex-wrap:wrap;font-size:13px\"><a href=\"/api/portfolio/profile\">/api/portfolio/profile</a> <a href=\"/lang/en.json\">/lang/en.json</a> <a href=\"/health\">/health</a></p>"
            + "</div></body></html>",
            "text/html"));
    }
}

app.MapControllers();

// Lightweight health endpoint for previews / uptime checks
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "Portfolio.Api", time = DateTimeOffset.UtcNow }));
app.Run();
