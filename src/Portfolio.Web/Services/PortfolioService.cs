using System.Net.Http.Json;
using Portfolio.Data.Models;

namespace Portfolio.Web.Services;

/// <summary>
/// Public API calls for the landing page.
/// </summary>
public class PortfolioService
{
    private readonly HttpClient _http;
    public PortfolioService(HttpClient http) => _http = http;

    public async Task<Profile?> GetProfileAsync() =>
        await _http.GetFromJsonAsync<Profile>("api/portfolio/profile");

    public async Task<List<Project>> GetProjectsAsync() =>
        await _http.GetFromJsonAsync<List<Project>>("api/portfolio/projects") ?? new();

    public async Task<(List<Skill> bars, List<Skill> tags)> GetSkillsAsync()
    {
        var resp = await _http.GetFromJsonAsync<SkillsResponse>("api/portfolio/skills");
        return (resp?.bars ?? new(), resp?.tags ?? new());
    }

    public async Task<List<Testimonial>> GetTestimonialsAsync() =>
        await _http.GetFromJsonAsync<List<Testimonial>>("api/portfolio/testimonials") ?? new();

    public async Task<bool> SubmitContactAsync(ContactMessage msg)
    {
        var resp = await _http.PostAsJsonAsync("api/portfolio/contact", msg);
        return resp.IsSuccessStatusCode;
    }

    private class SkillsResponse { public List<Skill> bars { get; set; } = new(); public List<Skill> tags { get; set; } = new(); }
}
