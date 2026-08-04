using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Portfolio.Web.Services;

/// <summary>
/// Custom AuthenticationStateProvider that reads JWT token claims.
/// </summary>
public class ApiAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _current = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_current));

    public void NotifyLoggedIn(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        _current = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_current)));
    }

    public void NotifyLoggedOut()
    {
        _current = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_current)));
    }
}
