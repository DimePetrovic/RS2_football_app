namespace Comeback.Profile.Application.Features.Profiles.Queries.GetAllUsers;

using Comeback.Profile.Application.DTOs;
using MediatR;

public sealed record GetAllUsersQuery(Guid ExcludeUserId) : IRequest<List<AdminUserListItem>>;
