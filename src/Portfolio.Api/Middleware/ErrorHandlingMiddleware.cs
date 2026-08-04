using Portfolio.Api.Services;
namespace Portfolio.Api.Middleware;
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ErrorLogService _err;
    public ErrorHandlingMiddleware(RequestDelegate next, ErrorLogService err) { _next=next;_err=err; }
    public async Task InvokeAsync(HttpContext ctx) {
        try { await _next(ctx); }
        catch(Exception ex) { await _err.LogAsync(ex,ctx.Request.Path+" ["+ctx.Request.Method+"]",500); ctx.Response.StatusCode=500; ctx.Response.ContentType="application/json";
            await ctx.Response.WriteAsync("{\"success\":false,\"message\":\"An unexpected error occurred. Admin notified.\"}"); }
    }
}
