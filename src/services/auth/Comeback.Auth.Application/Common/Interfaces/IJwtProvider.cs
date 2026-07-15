namespace Comeback.Auth.Application.Common.Interfaces;

using Comeback.Auth.Domain.Entities;

public interface IJwtProvider
{
    TokenPair Generate(ApplicationUser user);
}

public sealed record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
