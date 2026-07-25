namespace Comeback.Chat.Application.Tests.Commands;

using Comeback.BuildingBlocks.Application.Clients;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Application.Features.Messages.Commands.Send;
using Comeback.Chat.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

public sealed class SendMessageCommandHandlerTests
{
    private readonly IConversationRepository _repository = Substitute.For<IConversationRepository>();
    private readonly IChatUnitOfWork _unitOfWork = Substitute.For<IChatUnitOfWork>();
    private readonly IMessageEncryptionService _encryption = Substitute.For<IMessageEncryptionService>();
    private readonly IChatGroupClient _groupClient = Substitute.For<IChatGroupClient>();
    private readonly IPlayerInfoClient _playerInfo = Substitute.For<IPlayerInfoClient>();
    private readonly SendMessageCommandHandler _sut;

    public SendMessageCommandHandlerTests()
    {
        _encryption.Encrypt(Arg.Any<string>()).Returns("encrypted");
        _sut = new SendMessageCommandHandler(_repository, _unitOfWork, _encryption, _groupClient, _playerInfo);
    }

    [Fact]
    public async Task Handle_GroupMessage_PersistsResolvedProfileNameMatchingBroadcast()
    {
        var senderId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var conversation = Conversation.CreateGroup(groupId, "Ekipa", null);
        _repository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(conversation);
        _groupClient.GetGroupInfoAsync(groupId, Arg.Any<CancellationToken>())
            .Returns(new GroupChatInfo(groupId, "Ekipa", null,
                new[] { new GroupMemberInfo(senderId, "Token Ime") }));
        // Profile service resolves a different display name than the one in the auth token.
        _playerInfo.GetPlayerInfosAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<PlayerInfo> { new(senderId, "korisnik", null, "Profil Ime", null) });

        Message? saved = null;
        _repository.AddMessage(Arg.Do<Message>(m => saved = m));

        var result = await _sut.Handle(
            new SendMessageCommand(conversation.Id, senderId, "Token Ime", "cao"), CancellationToken.None);

        // The stored row and the broadcast DTO must carry the same (resolved) name.
        saved!.SenderDisplayName.Should().Be("Profil Ime");
        result.Message.SenderDisplayName.Should().Be("Profil Ime");
    }
}
