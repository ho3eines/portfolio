using System.Net.Http.Json;
using Portfolio.Data.DTOs;
using Portfolio.Data.Models;

namespace Portfolio.Web.Services;

/// <summary>
/// Admin CRUD API calls (requires JWT auth).
/// Uses ApiResponse unwrapping.
/// </summary>
public class AdminService
{
    private readonly HttpClient _http;
    public AdminService(HttpClient http) => _http = http;

    // --- Helper: unwrap ApiResponse<T> ---
    private async Task<T?> GetAsync<T>(string url) where T : class
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<T>>(url);
        return resp?.Data;
    }

    private async Task<List<T>> GetListAsync<T>(string url) where T : class
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<List<T>>>(url);
        return resp?.Data ?? new();
    }

    // --- Profile ---
    public async Task<Profile?> GetProfileAsync() => await GetAsync<Profile>("api/admin/profile");
    public async Task<bool> UpdateProfileAsync(Profile p) { var r = await _http.PutAsJsonAsync("api/admin/profile", p); return r.IsSuccessStatusCode; }

    // --- Projects ---
    public async Task<List<Project>> GetProjectsAsync() => await GetListAsync<Project>("api/admin/projects");
    public async Task<Project?> GetProjectAsync(int id) => await GetAsync<Project>($"api/admin/projects/{id}");
    public async Task<bool> CreateProjectAsync(Project p) { var r = await _http.PostAsJsonAsync("api/admin/projects", p); return r.IsSuccessStatusCode; }
    public async Task<bool> UpdateProjectAsync(int id, Project p) { var r = await _http.PutAsJsonAsync($"api/admin/projects/{id}", p); return r.IsSuccessStatusCode; }
    public async Task<bool> DeleteProjectAsync(int id) { var r = await _http.DeleteAsync($"api/admin/projects/{id}"); return r.IsSuccessStatusCode; }

    // --- Skills ---
    public async Task<List<Skill>> GetSkillsAsync() => await GetListAsync<Skill>("api/admin/skills");
    public async Task<bool> CreateSkillAsync(Skill s) { var r = await _http.PostAsJsonAsync("api/admin/skills", s); return r.IsSuccessStatusCode; }
    public async Task<bool> UpdateSkillAsync(int id, Skill s) { var r = await _http.PutAsJsonAsync($"api/admin/skills/{id}", s); return r.IsSuccessStatusCode; }
    public async Task<bool> DeleteSkillAsync(int id) { var r = await _http.DeleteAsync($"api/admin/skills/{id}"); return r.IsSuccessStatusCode; }

    // --- Experiences ---
    public async Task<List<Experience>> GetExperiencesAsync() => await GetListAsync<Experience>("api/admin/experiences");
    public async Task<bool> CreateExperienceAsync(Experience e) { var r = await _http.PostAsJsonAsync("api/admin/experiences", e); return r.IsSuccessStatusCode; }
    public async Task<bool> UpdateExperienceAsync(int id, Experience e) { var r = await _http.PutAsJsonAsync($"api/admin/experiences/{id}", e); return r.IsSuccessStatusCode; }
    public async Task<bool> DeleteExperienceAsync(int id) { var r = await _http.DeleteAsync($"api/admin/experiences/{id}"); return r.IsSuccessStatusCode; }

    // --- Testimonials ---
    public async Task<List<Testimonial>> GetTestimonialsAsync() => await GetListAsync<Testimonial>("api/admin/testimonials");
    public async Task<bool> CreateTestimonialAsync(Testimonial t) { var r = await _http.PostAsJsonAsync("api/admin/testimonials", t); return r.IsSuccessStatusCode; }
    public async Task<bool> UpdateTestimonialAsync(int id, Testimonial t) { var r = await _http.PutAsJsonAsync($"api/admin/testimonials/{id}", t); return r.IsSuccessStatusCode; }
    public async Task<bool> DeleteTestimonialAsync(int id) { var r = await _http.DeleteAsync($"api/admin/testimonials/{id}"); return r.IsSuccessStatusCode; }

    // --- Messages ---
    public async Task<List<ContactMessage>> GetMessagesAsync(bool? unreadOnly = null)
    {
        var url = "api/admin/messages";
        if (unreadOnly == true) url += "?unreadOnly=true";
        return await GetListAsync<ContactMessage>(url);
    }
    public async Task<bool> MarkReadAsync(int id) { var r = await _http.PutAsync($"api/admin/messages/{id}/read", null); return r.IsSuccessStatusCode; }
    public async Task<bool> DeleteMessageAsync(int id) { var r = await _http.DeleteAsync($"api/admin/messages/{id}"); return r.IsSuccessStatusCode; }

    // --- Resources ---
    public async Task<string[]> ListResourceFilesAsync()
    {
        var resp = await _http.GetFromJsonAsync<FileListResponse>("api/resources");
        return resp?.files ?? Array.Empty<string>();
    }
    public async Task<(bool ok, string msg)> ExecuteResourceAsync(string fileName)
    {
        var r = await _http.PostAsync($"api/resources/execute/{fileName}", null);
        var body = await r.Content.ReadAsStringAsync();
        return (r.IsSuccessStatusCode, body);
    }

    // NOTE: GET api/resources/history returns a RAW list of Resource (not ApiResponse-wrapped).
    public async Task<List<Resource>> GetResourceHistoryAsync()
    {
        try { return await _http.GetFromJsonAsync<List<Resource>>("api/resources/history") ?? new(); }
        catch { return new(); }
    }

    // --- Contracts ---
    public async Task<List<Contract>> GetContractsAsync() => await GetListAsync<Contract>("api/admin/contracts");

    private class FileListResponse { public string[]? files { get; set; } }
}
