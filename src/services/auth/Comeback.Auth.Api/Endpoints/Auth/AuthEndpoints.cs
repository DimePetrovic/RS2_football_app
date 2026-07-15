namespace Comeback.Auth.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterEndpoint.Handle).WithName("Register");
        group.MapGet("/validate-email-token", ValidateEmailTokenEndpoint.Handle).WithName("ValidateEmailToken");
        group.MapPost("/complete-registration", CompleteRegistrationEndpoint.Handle).WithName("CompleteRegistration");
        group.MapPost("/resend-confirmation", ResendConfirmationEmailEndpoint.Handle).WithName("ResendConfirmationEmail");
        group.MapPost("/login", LoginEndpoint.Handle).WithName("Login");
        group.MapPost("/refresh", RefreshTokenEndpoint.Handle).WithName("RefreshToken");
        group.MapPost("/revoke", RevokeTokenEndpoint.Handle).WithName("RevokeToken").RequireAuthorization();

        return app;
    }
}
