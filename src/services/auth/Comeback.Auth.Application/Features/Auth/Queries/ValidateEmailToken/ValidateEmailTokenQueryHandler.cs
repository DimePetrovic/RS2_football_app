namespace Comeback.Auth.Application.Features.Auth.Queries.ValidateEmailToken;

using System.Text;
using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;
using Comeback.BuildingBlocks.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

internal sealed class ValidateEmailTokenQueryHandler : IRequestHandler<ValidateEmailTokenQuery, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ValidateEmailTokenQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> Handle(ValidateEmailTokenQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found.", "user.not_found");

        if (user.AccountStatus != AccountStatus.PendingEmailVerification)
            throw new ConflictException("Account is already active.", "account.already_active");

        var rawToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));

        return await _userManager.VerifyUserTokenAsync(
            user,
            _userManager.Options.Tokens.EmailConfirmationTokenProvider,
            "EmailConfirmation",
            rawToken);
    }
}
