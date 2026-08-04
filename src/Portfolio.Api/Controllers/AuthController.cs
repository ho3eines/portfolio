using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Services;
using Portfolio.Data;
using Portfolio.Data.DTOs;
using Portfolio.Data.Models;

namespace Portfolio.Api.Controllers;

[ApiController, Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SqlConnectionFactory _db;
    private readonly TokenService _token;
    public AuthController(SqlConnectionFactory db, TokenService token) { _db = db; _token = token; }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var hash = HashPassword(req.Password);
        using var c = _db.Create();
        var user = await c.QueryFirstOrDefaultAsync<User>(@"SELECT u.*, r.Name AS RoleName FROM Users u JOIN Roles r ON u.RoleId=r.Id WHERE u.Username=@U AND u.PasswordHash=@H AND u.IsActive=1", new { U = req.Username, H = hash });
        if (user == null) return Unauthorized(ApiResponse<object>.Fail("Invalid credentials"));

        var (t, exp) = _token.GenerateAccessToken(user.Id, user.Username, user.RoleName ?? "Admin");
        var refresh = _token.GenerateRefreshToken();
        await c.ExecuteAsync("UPDATE Users SET RefreshToken=@R, RefreshTokenExp=DATEADD(DAY,7,SYSUTCDATETIME()), LastLoginAt=SYSUTCDATETIME() WHERE Id=@Id", new { R = refresh, user.Id });
        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse { Token = t, RefreshToken = refresh, FullName = user.FullName ?? user.Username, Role = user.RoleName ?? "Admin", ExpiresAt = exp }));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        using var c = _db.Create();
        if (await c.QueryFirstOrDefaultAsync<User>("SELECT 1 FROM Users WHERE Username=@U", new { U = req.Username }) != null) return BadRequest(ApiResponse<object>.Fail("Username taken"));
        await c.ExecuteAsync("INSERT INTO Users (Username,Email,PasswordHash,FullName,RoleId) VALUES (@U,@E,@H,@F,3)", new { U = req.Username, E = req.Email, H = HashPassword(req.Password), F = req.FullName });
        return Ok(ApiResponse<object>.Ok(new { }, "Registration successful"));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        using var c = _db.Create();
        var user = await c.QueryFirstOrDefaultAsync<User>("SELECT u.*, r.Name AS RoleName FROM Users u JOIN Roles r ON u.RoleId=r.Id WHERE u.RefreshToken=@R AND u.RefreshTokenExp>SYSUTCDATETIME() AND u.IsActive=1", new { R = req.RefreshToken });
        if (user == null) return Unauthorized(ApiResponse<object>.Fail("Invalid refresh token"));
        var (t, exp) = _token.GenerateAccessToken(user.Id, user.Username, user.RoleName ?? "Admin");
        var refresh = _token.GenerateRefreshToken();
        await c.ExecuteAsync("UPDATE Users SET RefreshToken=@R,RefreshTokenExp=DATEADD(DAY,7,SYSUTCDATETIME()) WHERE Id=@Id", new { R = refresh, user.Id });
        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse { Token = t, RefreshToken = refresh, FullName = user.FullName ?? user.Username, Role = user.RoleName ?? "Admin", ExpiresAt = exp }));
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        using var c = _db.Create();
        var u = await c.QueryFirstOrDefaultAsync<User>("SELECT u.*, r.Name AS RoleName FROM Users u JOIN Roles r ON u.RoleId=r.Id WHERE u.Id=@Id", new { Id = id });
        return u == null ? NotFound() : Ok(new { u.Id, u.Username, u.Email, u.FullName, u.AvatarUrl, Role = u.RoleName });
    }

    private static string HashPassword(string p) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(p + "PortfolioSalt2026!"))).ToLower();
}
