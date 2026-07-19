namespace Comeback.Match.Api.IntegrationTests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>Generates a JWT compatible with the dev settings from Match appsettings.json.</summary>
public static class TestTokens
{
    // Poklapa se sa JwtSettings u src/services/match/.../appsettings.json.
    private const string Secret = "dev-secret-min-32-chars-comeback-auth-2026";
    private const string Issuer = "comeback-auth";
    private const string Audience = "comeback-api";

    public static string For(Guid userId, string displayName = "Test Player", string role = "Player")
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Role, role),
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
