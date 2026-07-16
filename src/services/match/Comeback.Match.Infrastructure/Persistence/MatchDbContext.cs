namespace Comeback.Match.Infrastructure.Persistence;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class MatchDbContext : DbContext, IMatchUnitOfWork
{
    public DbSet<Domain.Entities.Match> Matches => Set<Domain.Entities.Match>();
    public DbSet<MatchParticipant> Participants => Set<MatchParticipant>();
    public DbSet<MatchPlayerReview> Reviews => Set<MatchPlayerReview>();
    public DbSet<MatchMedia> Media => Set<MatchMedia>();

    public MatchDbContext(DbContextOptions<MatchDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatchDbContext).Assembly);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);
}
