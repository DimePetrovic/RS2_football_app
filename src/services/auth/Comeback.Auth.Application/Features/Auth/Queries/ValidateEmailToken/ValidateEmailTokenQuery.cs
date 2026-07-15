namespace Comeback.Auth.Application.Features.Auth.Queries.ValidateEmailToken;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record ValidateEmailTokenQuery(
    string UserId,
    string Token) : IQuery<bool>;
