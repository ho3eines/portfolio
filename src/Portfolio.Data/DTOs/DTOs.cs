using System.ComponentModel.DataAnnotations;

namespace Portfolio.Data.DTOs;

// ================ AUTH ================
public class LoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class RegisterRequest
{
    [Required, MaxLength(100)] public string Username { get; set; } = string.Empty;
    [Required, MaxLength(300), EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    [MaxLength(200)] public string? FullName { get; set; }
}

public class RefreshRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

// ================ RESPONSES ================
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    public static ApiResponse<T> Ok(T data, string? msg = null) => new() { Success = true, Data = data, Message = msg };
    public static ApiResponse<T> Fail(string msg) => new() { Success = false, Message = msg };
}

// ================ DASHBOARD ================
public class DashboardStats
{
    public int TotalProjects { get; set; }
    public int TotalSkills { get; set; }
    public int TotalMessages { get; set; }
    public int UnreadMessages { get; set; }
    public int TotalTestimonials { get; set; }
}
