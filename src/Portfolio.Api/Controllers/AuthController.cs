using System.Security.Cryptography;
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
        using var c = _db.Create();
        // PBKDF2 hashes are salted, so fetch the user by username first and
        // verify the password against the stored hash.
        var user = await c.QueryFirstOrDefaultAsync<User>(@"SELECT u.*, r.Name AS RoleName FROM Users u JOIN Roles r ON u.RoleId=r.Id WHERE u.Username=@U AND u.IsActive=1", new { U = req.Username });
        if (user == null || !VerifyPassword(req.Password, user.PasswordHash)) return Unauthorized(ApiResponse<object>.Fail("Invalid credentials"));

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

    // ── Password hashing: PBKDF2 (SHA-256, 100k iterations, per-user salt) ──
    // Stored format: PBKDF2$iterations$saltHex$hashHex
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"PBKDF2${Pbkdf2Iterations}${Convert.ToHexString(salt)}${Convert.ToHexString(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        try
        {
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2") return false;
            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromHexString(parts[2]);
            var expected = Convert.FromHexString(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false; // malformed/legacy hash → treat as invalid, never crash
        }
    }
}
