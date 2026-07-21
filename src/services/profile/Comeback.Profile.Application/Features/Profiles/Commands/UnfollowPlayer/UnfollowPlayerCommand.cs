namespace Comeback.Profile.Application.Features.Profiles.Commands.UnfollowPlayer;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record UnfollowPlayerCommand(Guid FollowerUserId, Guid FollowedUserId) : ICommand;
