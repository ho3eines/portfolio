using Portfolio.Api.Services;
namespace Portfolio.Api.Middleware;
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    public ErrorHandlingMiddleware(RequestDelegate next) { _next=next; }
    public async Task InvokeAsync(HttpContext ctx, ErrorLogService err) {
        try { await _next(ctx); }
        catch(Exception ex) { await err.LogAsync(ex,ctx.Request.Path+" ["+ctx.Request.Method+"]",500); ctx.Response.StatusCode=500; ctx.Response.ContentType="application/json";
            await ctx.Response.WriteAsync("{\"success\":false,\"message\":\"An unexpected error occurred. Admin notified.\"}"); }
    }
}
