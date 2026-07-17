namespace Comeback.Profile.Application.Features.Profiles.Queries.GetAllUserIds;
using MediatR;

public sealed record GetAllUserIdsQuery : IRequest<List<Guid>>;
