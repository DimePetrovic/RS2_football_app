namespace Comeback.Auth.Application.DTOs;

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    Guid UserId,
    string Username,
    string Email,
    string Role);
