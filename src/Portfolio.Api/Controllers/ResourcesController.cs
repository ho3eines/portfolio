using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Data;
using Portfolio.Data.DTOs;
using Portfolio.Data.Models;

namespace Portfolio.Api.Controllers;

[ApiController, Route("api/resources")]
public class ResourcesController : ControllerBase
{
    private readonly SqlConnectionFactory _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ResourcesController> _log;
    public ResourcesController(SqlConnectionFactory db, IWebHostEnvironment env, ILogger<ResourcesController> log) { _db=db;_env=env;_log=log; }

    [HttpGet] public IActionResult ListFiles() { var dir=FindPath();if(!Directory.Exists(dir))return Ok(new{files=Array.Empty<string>()});return Ok(new{files=Directory.GetFiles(dir,"*.sql").Select(Path.GetFileName).OrderBy(f=>f).ToList(),directory=dir}); }

    [HttpPost("execute/{fileName}"),Authorize(Roles="Admin,Editor")]
    public async Task<IActionResult> Execute(string fileName) {
        var dir=FindPath();var path=Path.Combine(dir,fileName);if(!System.IO.File.Exists(path))return NotFound(new{error=$"File '{fileName}' not found."});
        var sql=await System.IO.File.ReadAllTextAsync(path);
        using var c=_db.Create();var existing=await c.QueryFirstOrDefaultAsync<Resource>("SELECT * FROM Resources WHERE FileName=@F",new{F=fileName});var now=DateTime.UtcNow;
        try{
            // SQL files use "GO" batch separators (an SSMS/SQLCMD command, not T-SQL).
            // Dapper cannot parse "GO", so split the file into batches and run each one.
            foreach (var batch in SplitBatches(sql))
                await c.ExecuteAsync(batch);
            if(existing==null)await c.ExecuteAsync("INSERT INTO Resources(FileName,FileContent,IsSuccess,ExecutedAt,CreatedAt,UpdatedAt) VALUES(@F,@S,1,@N,@N,@N)",new{F=fileName,S=sql,N=now});
            else await c.ExecuteAsync("UPDATE Resources SET FileContent=@S,IsSuccess=1,ErrorMessage=NULL,ExecutedAt=@N,UpdatedAt=@N WHERE FileName=@F",new{F=fileName,S=sql,N=now});
            _log.LogInformation("Executed: {File}",fileName);return Ok(ApiResponse<object>.Ok(new{fileName,status="success"}));}
        catch(Exception ex){
            var err=$"FILE: {fileName} | {ex.Message}";if(ex.InnerException!=null)err+=$" | Inner: {ex.InnerException.Message}";
            _log.LogError(ex,"Resource failed: {File}",fileName);
            if(existing==null)await c.ExecuteAsync("INSERT INTO Resources(FileName,FileContent,IsSuccess,ErrorMessage,ExecutedAt,CreatedAt,UpdatedAt) VALUES(@F,@S,0,@E,@N,@N,@N)",new{F=fileName,S=sql,E=err,N=now});
            else await c.ExecuteAsync("UPDATE Resources SET FileContent=@S,IsSuccess=0,ErrorMessage=@E,ExecutedAt=@N,UpdatedAt=@N WHERE FileName=@F",new{F=fileName,S=sql,E=err,N=now});
            return StatusCode(500,ApiResponse<object>.Fail(err));}
    }

    [HttpPost("execute-all"),Authorize(Roles="Admin")]
    public async Task<IActionResult> ExecuteAll(){var dir=FindPath();if(!Directory.Exists(dir))return NotFound();var files=Directory.GetFiles(dir,"*.sql").OrderBy(f=>f).Select(Path.GetFileName).ToList();var results=new List<object>();using var c=_db.Create();foreach(var f in files){try{var sql=await System.IO.File.ReadAllTextAsync(Path.Combine(dir,f));foreach(var batch in SplitBatches(sql))await c.ExecuteAsync(batch);results.Add(new{file=f,status="ok"});}catch(Exception ex){results.Add(new{file=f,status="err",error=ex.Message});}}return Ok(new{results,totalRan=files.Count});}

    [HttpGet("history")] public async Task<IActionResult> History() { using var c=_db.Create();var items=await c.QueryAsync<Resource>("SELECT * FROM Resources ORDER BY ExecutedAt DESC");return Ok(items); }

    private string FindPath(){var w=_env.WebRootPath??Path.Combine(_env.ContentRootPath,"wwwroot");var p=Path.Combine(w,"resources");if(Directory.Exists(p))return p;
    var a=Path.GetFullPath(Path.Combine(_env.ContentRootPath,"..","Portfolio.Web","wwwroot","resources"));if(Directory.Exists(a))return a;
    var r=Path.GetFullPath(Path.Combine(_env.ContentRootPath,"..",".."));var rp=Path.Combine(r,"wwwroot","resources");if(Directory.Exists(rp))return rp;return p;}

    private static string[] SplitBatches(string sql) =>
        System.Text.RegularExpressions.Regex.Split(sql, @"(?im)^\s*GO\s*$")
            .Select(b => b.Trim()).Where(b => b.Length > 0).ToArray();
}
