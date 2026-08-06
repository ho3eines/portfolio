using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Portfolio.Data.DTOs;

namespace Portfolio.Web.Services;

/// <summary>
/// Handles JWT auth: login, register, token refresh, token storage.
/// </summary>
public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly ApiAuthStateProvider _authState;

    public AuthService(HttpClient http, ILocalStorageService localStorage, AuthenticationStateProvider authState)
    { _http = http; _localStorage = localStorage; _authState = (ApiAuthStateProvider)authState; }

    public async Task<LoginResponse?> Login(string username, string password)
    {
        var result = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest { Username = username, Password = password });
        if (!result.IsSuccessStatusCode) return null;
        var resp = await result.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        if (resp?.Data == null) return null;

        await _localStorage.SetItemAsync("token", resp.Data.Token);
        await _localStorage.SetItemAsync("refreshToken", resp.Data.RefreshToken);
        _authState.NotifyLoggedIn(resp.Data.Token);
        SetToken(resp.Data.Token);
        return resp.Data;
    }

    public async Task<bool> Register(string username, string email, string password, string? fullName)
    {
        var result = await _http.PostAsJsonAsync("api/auth/register", new RegisterRequest { Username = username, Email = email, Password = password, FullName = fullName });
        return result.IsSuccessStatusCode;
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("token");
        await _localStorage.RemoveItemAsync("refreshToken");
        _authState.NotifyLoggedOut();
        RemoveToken();
    }

    public async Task InitializeAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("token");
        if (!string.IsNullOrEmpty(token))
        {
            SetToken(token);
            _authState.NotifyLoggedIn(token);
        }
    }

    public void SetToken(string token) => _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    public void RemoveToken() => _http.DefaultRequestHeaders.Authorization = null;
}
