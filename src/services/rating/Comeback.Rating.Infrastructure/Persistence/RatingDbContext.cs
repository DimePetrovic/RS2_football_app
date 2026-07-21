namespace Comeback.Rating.Infrastructure.Persistence;

using Comeback.Rating.Application.Common.Interfaces;
using Comeback.Rating.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class RatingDbContext : DbContext, IUnitOfWork
{
    public RatingDbContext(DbContextOptions<RatingDbContext> options) : base(options) { }

    public DbSet<PlayerXp> PlayerXps => Set<PlayerXp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RatingDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await base.SaveChangesAsync(cancellationToken);
}
