using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Portfolio.Api.Services;
public class TokenService
{
    private readonly IConfiguration _cfg;
    public TokenService(IConfiguration cfg) => _cfg = cfg;
    public (string token, DateTime expires) GenerateAccessToken(int userId, string username, string role) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(_cfg["Jwt:ExpireMinutes"] ?? "120"));
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, role) };
        var token = new JwtSecurityToken(issuer: _cfg["Jwt:Issuer"], audience: _cfg["Jwt:Audience"], claims: claims, expires: expires, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
    public string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
