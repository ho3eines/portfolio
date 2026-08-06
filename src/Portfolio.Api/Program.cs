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

// Support serving static frontend assets (/lang/*.json, css, js) if requested against the API host
var webWwwRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "Portfolio.Web", "wwwroot"));
if (Directory.Exists(webWwwRoot))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webWwwRoot);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
}

app.MapControllers();
app.Run();
