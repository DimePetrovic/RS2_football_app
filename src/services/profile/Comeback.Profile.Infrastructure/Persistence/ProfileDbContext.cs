namespace Comeback.Profile.Infrastructure.Persistence;

using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

public sealed class ProfileDbContext : DbContext, IUnitOfWork
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options) { }

    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<PlayerGroup> Groups => Set<PlayerGroup>();
    public DbSet<PlayerGroupMember> GroupMembers => Set<PlayerGroupMember>();
    public DbSet<PlayerFollow> PlayerFollows => Set<PlayerFollow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfileDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
