namespace Comeback.Profile.Application.Features.Profiles.Commands.FollowPlayer;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record FollowPlayerCommand(Guid FollowerUserId, Guid FollowedUserId) : ICommand;
