namespace Comeback.Profile.Application.Features.Groups.Queries.GetGroupMatchInfo;

using Comeback.Profile.Application.DTOs;
using MediatR;

public sealed record GetGroupMatchInfoQuery(Guid GroupId) : IRequest<GroupMatchInfoResponse>;
