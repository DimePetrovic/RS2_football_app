namespace Comeback.Chat.Infrastructure.Persistence;

using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class ChatDbContext : DbContext, IChatUnitOfWork
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<HiddenMessage> HiddenMessages => Set<HiddenMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);

    public new Task SaveChangesAsync(CancellationToken ct = default)
        => base.SaveChangesAsync(ct);
}
