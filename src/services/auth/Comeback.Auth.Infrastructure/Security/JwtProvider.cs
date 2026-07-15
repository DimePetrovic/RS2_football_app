namespace Comeback.Auth.Infrastructure.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Comeback.Auth.Application.Common.Interfaces;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

internal sealed class JwtProvider : IJwtProvider
{
    private readonly JwtSettings _settings;

    public JwtProvider(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public TokenPair Generate(ApplicationUser user)
    {
        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);

        return new TokenPair(
            GenerateAccessToken(user, accessTokenExpiresAt),
            accessTokenExpiresAt,
            GenerateRefreshToken(),
            refreshTokenExpiresAt);
    }

    private string GenerateAccessToken(ApplicationUser user, DateTime expiresAt)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
