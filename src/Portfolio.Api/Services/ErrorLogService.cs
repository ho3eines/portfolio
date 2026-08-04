using Dapper;
using Portfolio.Data;
using System.Security.Claims;

namespace Portfolio.Api.Services;

public class ErrorLogService
{
    private readonly SqlConnectionFactory _conn;
    private readonly ILogger<ErrorLogService> _log;
    private readonly IHttpContextAccessor? _http;

    public ErrorLogService(SqlConnectionFactory conn, ILogger<ErrorLogService> log, IHttpContextAccessor? http = null)
    { _conn = conn; _log = log; _http = http; }

    public async Task LogAsync(Exception ex, string source, int? code = 500, string level = "Error")
    {
        var msg = ex.Message; var inner = ex.InnerException;
        while (inner != null) { msg += " | Inner: " + inner.Message; inner = inner.InnerException; }
        var stack = ex.ToString(); if (stack.Length > 4000) stack = stack[..4000];
        var ctx = _http?.HttpContext;
        var uidStr = ctx?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? uid = int.TryParse(uidStr, out var u) ? u : null;
        _log.LogError(ex, "[{Src}] {Msg}", source, ex.Message);

        try
        {
            using var db = _conn.Create();
            await db.ExecuteAsync(@"INSERT INTO ErrorLogs (Level,Source,Message,StackTrace,StatusCode,UserId,RequestPath,Method,UserAgent,IpAddress,[Timestamp],CreatedAt)
                VALUES (@Level,@Source,@Message,@StackTrace,@StatusCode,@UserId,@RequestPath,@Method,@UserAgent,@IpAddress,SYSUTCDATETIME(),SYSUTCDATETIME())",
                new { Level = level, Source = Trunc(source, 300), Message = Trunc(msg, 2000), StackTrace = stack, StatusCode = code, UserId = uid,
                    RequestPath = Trunc(ctx?.Request.Path.ToString(), 500), Method = Trunc(ctx?.Request.Method, 10),
                    UserAgent = Trunc(ctx?.Request.Headers["User-Agent"].FirstOrDefault(), 500),
                    IpAddress = Trunc(ctx?.Connection.RemoteIpAddress?.ToString(), 50) });
        }
        catch (Exception dbe) { _log.LogCritical(dbe, "DB error log failed"); }
    }

    public async Task LogWarningAsync(string msg, string source)
    {
        _log.LogWarning("[{Src}] {Msg}", source, msg);
        try { using var db = _conn.Create(); await db.ExecuteAsync("INSERT INTO ErrorLogs (Level,Source,Message,Timestamp,CreatedAt) VALUES ('Warning',@Src,@Msg,SYSUTCDATETIME(),SYSUTCDATETIME())", new { Src = Trunc(source, 300), Msg = Trunc(msg, 2000) }); } catch { }
    }

    private static string? Trunc(string? s, int max) => string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..(max - 3)] + "...";
}
