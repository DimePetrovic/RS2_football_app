namespace Comeback.Social.Infrastructure.Persistence;

using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class SocialDbContext : DbContext, ISocialUnitOfWork
{
    public SocialDbContext(DbContextOptions<SocialDbContext> options) : base(options) { }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<UserFeedItem> UserFeedItems => Set<UserFeedItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialDbContext).Assembly);
    }
}
