using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Services;
using Portfolio.Data;
using Portfolio.Data.Models;

namespace Portfolio.Api.Controllers;

[ApiController, Route("api/admin/errors"), Authorize(Roles="Admin")]
public class ErrorLogController : ControllerBase
{
    private readonly SqlConnectionFactory _db;
    private readonly ErrorLogService _err;
    public ErrorLogController(SqlConnectionFactory db, ErrorLogService err) { _db=db;_err=err; }

    [HttpGet] public async Task<IActionResult> GetAll(string? level, bool? unresolved, int page=1, int pageSize=50) {
        using var c=_db.Create();var where="";if(!string.IsNullOrEmpty(level))where+=" AND Level=@Lvl";if(unresolved==true)where+=" AND IsResolved=0";
        var total=await c.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM ErrorLogs WHERE 1=1{where}",new{Lvl=level});
        var items=(await c.QueryAsync<ErrorLog>($"SELECT * FROM ErrorLogs WHERE 1=1{where} ORDER BY [Timestamp] DESC OFFSET @Off ROWS FETCH NEXT @Sz ROWS ONLY",new{Lvl=level,Off=(page-1)*pageSize,Sz=pageSize})).ToList();
        var unres=await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ErrorLogs WHERE IsResolved=0");
        return Ok(new{total,page,pageSize,unresolved=unres,items});}
    [HttpPut("{id}/resolve")] public async Task<IActionResult> Resolve(int id){using var c=_db.Create();await c.ExecuteAsync("UPDATE ErrorLogs SET IsResolved=1,ResolvedAt=SYSUTCDATETIME(),ResolvedBy=@U WHERE Id=@Id",new{U=User.Identity?.Name??"admin",Id=id});return Ok(new{status="resolved"});}
    [HttpPut("resolve-all")] public async Task<IActionResult> ResolveAll(){using var c=_db.Create();var n=await c.ExecuteAsync("UPDATE ErrorLogs SET IsResolved=1,ResolvedAt=SYSUTCDATETIME(),ResolvedBy=@U WHERE IsResolved=0",new{U=User.Identity?.Name??"admin"});return Ok(new{resolved=n});}
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id){using var c=_db.Create();await c.ExecuteAsync("DELETE FROM ErrorLogs WHERE Id=@Id",new{Id=id});return Ok(new{status="deleted"});}
    [HttpDelete("clear")] public async Task<IActionResult> ClearAll(){using var c=_db.Create();var n=await c.ExecuteAsync("DELETE FROM ErrorLogs");return Ok(new{deleted=n});}
    [HttpPost("test")] public async Task<IActionResult> Test(){await _err.LogAsync(new Exception("Test error from admin panel"),"ErrorLogController.Test");return Ok(new{status="test error logged"});}
}
