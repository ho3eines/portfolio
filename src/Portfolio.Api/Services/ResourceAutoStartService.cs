using Dapper;
using Portfolio.Data;

namespace Portfolio.Api.Services;

public class ResourceAutoStartService : IHostedService
{
    private readonly SqlConnectionFactory _conn;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ResourceAutoStartService> _log;

    public ResourceAutoStartService(SqlConnectionFactory conn, IWebHostEnvironment env, ILogger<ResourceAutoStartService> log)
    { _conn = conn; _env = env; _log = log; }

    public async Task StartAsync(CancellationToken ct)
    {
        _log.LogInformation("Resource Auto-Start: scanning wwwroot/resources/...");
        var dir = FindResourcesPath();
        if (!Directory.Exists(dir)) { _log.LogWarning("wwwroot/resources not found at: {Path}", dir); return; }

        var files = Directory.GetFiles(dir, "*.sql").OrderBy(f => f).ToList();
        if (files.Count == 0) { _log.LogInformation("No .sql files found."); return; }
        _log.LogInformation("Found {Count} .sql file(s): {Files}", files.Count, string.Join(", ", files.Select(Path.GetFileName)));

        int executed = 0, skipped = 0, failed = 0;
        using var db = _conn.Create();

        // ── Ensure the Resources tracking table exists BEFORE we query it. ──
        // On a fresh DB the table doesn't exist yet (01-create-database.sql creates it),
        // so without this guard the `SELECT * FROM Resources` below would throw
        // "Invalid object name 'Resources'" and crash the whole app at startup.
        await db.ExecuteAsync(@"
IF OBJECT_ID('dbo.Resources','U') IS NULL
CREATE TABLE [dbo].[Resources](
    [Id]           INT IDENTITY(1,1) NOT NULL,
    [FileName]     NVARCHAR(500)    NOT NULL,
    [FileContent]  NVARCHAR(MAX)    NULL,
    [ExecutedAt]   DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [IsSuccess]    BIT              NOT NULL DEFAULT 0,
    [ErrorMessage] NVARCHAR(MAX)    NULL,
    [CreatedAt]    DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]    DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Resources] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Resources_FileName] UNIQUE ([FileName])
);");

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath); ct.ThrowIfCancellationRequested();
            var existing = await db.QueryFirstOrDefaultAsync<Portfolio.Data.Models.Resource>("SELECT * FROM Resources WHERE FileName=@Name", new { Name = fileName });
            if (existing != null && existing.IsSuccess) { _log.LogInformation("Skipping '{File}' — already executed", fileName); skipped++; continue; }

            _log.LogInformation("Executing '{File}'...", fileName);
            try
            {
                var sql = await File.ReadAllTextAsync(filePath, ct);
                // SQL files use "GO" batch separators (an SSMS/SQLCMD command, not T-SQL).
                // Dapper cannot parse "GO", so split the file into statements and run each one.
                var batches = System.Text.RegularExpressions.Regex.Split(sql, @"(?im)^\s*GO\s*$")
                                .Select(b => b.Trim()).Where(b => b.Length > 0).ToArray();
                if (batches.Length == 0) { _log.LogWarning("'{File}' has no executable statements — skipping", fileName); skipped++; continue; }
                foreach (var batch in batches)
                    await db.ExecuteAsync(batch);
                var now = DateTime.UtcNow;
                if (existing != null)
                    await db.ExecuteAsync("UPDATE Resources SET FileContent=@Sql,IsSuccess=1,ErrorMessage=NULL,ExecutedAt=@Now,UpdatedAt=@Now WHERE FileName=@Name", new { Sql = sql, Now = now, Name = fileName });
                else
                    await db.ExecuteAsync("INSERT INTO Resources (FileName,FileContent,IsSuccess,ExecutedAt,CreatedAt,UpdatedAt) VALUES (@Name,@Sql,1,@Now,@Now,@Now)", new { Name = fileName, Sql = sql, Now = now });
                executed++; _log.LogInformation("✓ '{File}' executed", fileName);
            }
            catch (Exception ex)
            {
                failed++;
                var err = $"ERR [{DateTime.UtcNow:O}] {ex.GetType().Name}: {ex.Message}";
                if (ex.InnerException != null) err += $"\n  Inner: {ex.InnerException.Message}";
                _log.LogError(ex, "✗ '{File}' FAILED", fileName);
                var sql = ""; try { sql = await File.ReadAllTextAsync(filePath, ct); } catch { }
                var now = DateTime.UtcNow;
                if (existing != null)
                    await db.ExecuteAsync("UPDATE Resources SET FileContent=@Sql,IsSuccess=0,ErrorMessage=@Err,ExecutedAt=@Now,UpdatedAt=@Now WHERE FileName=@Name", new { Sql = sql, Err = err, Now = now, Name = fileName });
                else
                    await db.ExecuteAsync("INSERT INTO Resources (FileName,FileContent,IsSuccess,ErrorMessage,ExecutedAt,CreatedAt,UpdatedAt) VALUES (@Name,@Sql,0,@Err,@Now,@Now,@Now)", new { Name = fileName, Sql = sql, Err = err, Now = now });
            }
        }
        _log.LogInformation("✅ Resource Auto-Start: {Executed} exec, {Skipped} skip, {Failed} fail", executed, skipped, failed);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private string FindResourcesPath()
    {
        var wwwroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var path = Path.Combine(wwwroot, "resources"); if (Directory.Exists(path)) return path;
        var alt = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "Portfolio.Web", "wwwroot", "resources")); if (Directory.Exists(alt)) return alt;
        var repo = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "..")); var rp = Path.Combine(repo, "wwwroot", "resources"); if (Directory.Exists(rp)) return rp;
        return path;
    }
}
