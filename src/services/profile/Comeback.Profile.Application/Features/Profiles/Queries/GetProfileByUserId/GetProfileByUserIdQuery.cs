namespace Comeback.Profile.Application.Features.Profiles.Queries.GetProfileByUserId;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Profile.Application.DTOs;

public sealed record GetProfileByUserIdQuery(Guid UserId) : IQuery<ProfileResponse>;
