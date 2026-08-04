using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Data;
using Portfolio.Data.Models;

namespace Portfolio.Api.Controllers;

[ApiController, Route("/")]
public class SeoController : ControllerBase
{
    private readonly SqlConnectionFactory _db;
    public SeoController(SqlConnectionFactory db) => _db = db;

    [HttpGet("robots.txt"), Produces("text/plain")]
    public async Task<IActionResult> RobotsTxt() { using var c=_db.Create();var p=await c.QueryFirstOrDefaultAsync<Profile>("SELECT * FROM Profile");var site=p?.SiteUrl??"https://mahbodpour.com";var sb=new StringBuilder();sb.AppendLine("User-agent: *");sb.AppendLine("Allow: /");sb.AppendLine("Disallow: /admin");sb.AppendLine("Disallow: /login");sb.AppendLine("Disallow: /register");sb.AppendLine($"Sitemap: {site}/sitemap.xml");return Content(sb.ToString(),"text/plain",Encoding.UTF8);}

    [HttpGet("sitemap.xml"), Produces("application/xml")]
    public async Task<IActionResult> SitemapXml() { using var c=_db.Create();var p=await c.QueryFirstOrDefaultAsync<Profile>("SELECT * FROM Profile");var site=p?.SiteUrl??"https://mahbodpour.com";var projects=(await c.QueryAsync<Project>("SELECT * FROM Projects WHERE IsPublished=1 ORDER BY SortOrder")).ToList();var sb=new StringBuilder();sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");sb.AppendLine($"<url><loc>{site}/</loc><changefreq>weekly</changefreq><priority>1.0</priority></url>");foreach(var pr in projects)sb.AppendLine($"<url><loc>{site}/projects/{pr.Slug}</loc><changefreq>monthly</changefreq><priority>0.8</priority></url>");sb.AppendLine("</urlset>");return Content(sb.ToString(),"application/xml",Encoding.UTF8);}

    [HttpGet("api/seo-info")] public async Task<IActionResult> SeoInfo() { using var c=_db.Create();var p=await c.QueryFirstOrDefaultAsync<Profile>("SELECT * FROM Profile");if(p==null)return NotFound();return Ok(new{title=$"{p.FullName} — {p.Title}",description=p.MetaDescription??p.Bio,keywords=p.MetaKeywords,ogImage=p.OgImageUrl,siteUrl=p.SiteUrl});}
}
