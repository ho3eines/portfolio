using Dapper;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Data;
using Portfolio.Data.Models;

namespace Portfolio.Api.Controllers;

[ApiController, Route("api/portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly SqlConnectionFactory _db;
    public PortfolioController(SqlConnectionFactory db) => _db = db;

    [HttpGet("profile")] public async Task<IActionResult> GetProfile() { using var c=_db.Create(); var p=await c.QueryFirstOrDefaultAsync<Profile>("SELECT * FROM Profile"); return p==null?NotFound():Ok(p); }
    [HttpGet("projects")] public async Task<IActionResult> GetProjects() { using var c=_db.Create(); return Ok((await c.QueryAsync<Project>("SELECT * FROM Projects WHERE IsPublished=1 ORDER BY SortOrder")).ToList()); }
    [HttpGet("skills")] public async Task<IActionResult> GetSkills() {
        using var c=_db.Create(); var all=(await c.QueryAsync<Skill>("SELECT * FROM Skills ORDER BY SortOrder")).ToList();
        return Ok(new { bars=all.Where(s=>!s.IsTag).ToList(), tags=all.Where(s=>s.IsTag).ToList() }); }
    [HttpGet("experiences")] public async Task<IActionResult> GetExperiences() { using var c=_db.Create(); return Ok((await c.QueryAsync<Experience>("SELECT * FROM Experiences ORDER BY SortOrder")).ToList()); }
    [HttpGet("testimonials")] public async Task<IActionResult> GetTestimonials() { using var c=_db.Create(); return Ok((await c.QueryAsync<Testimonial>("SELECT * FROM Testimonials WHERE IsPublished=1 ORDER BY SortOrder")).ToList()); }
    [HttpGet("settings")] public async Task<IActionResult> GetSettings() { using var c=_db.Create(); var items=await c.QueryAsync<SiteSetting>("SELECT * FROM SiteSettings"); return Ok(items.ToDictionary(x=>x.Key, x=>(object?)x.Value)); }
    [HttpGet("seo")] public async Task<IActionResult> GetSeo() { using var c=_db.Create(); var p=await c.QueryFirstOrDefaultAsync<Profile>("SELECT * FROM Profile"); if(p==null)return NotFound(); return Ok(new{title=$"{p.FullName} — {p.Title}",description=p.MetaDescription??p.Bio,keywords=p.MetaKeywords,ogImage=p.OgImageUrl,siteUrl=p.SiteUrl,author=p.FullName}); }
    [HttpPost("contact")] public async Task<IActionResult> SubmitContact([FromBody] ContactMessage msg) { using var c=_db.Create(); msg.CreatedAt=DateTime.UtcNow; var id=await c.QuerySingleAsync<int>("INSERT INTO ContactMessages (Name,Email,Subject,Message,CreatedAt) OUTPUT INSERTED.Id VALUES (@Name,@Email,@Subject,@Message,SYSUTCDATETIME())", msg); return Ok(new{status="received",id}); }
}
