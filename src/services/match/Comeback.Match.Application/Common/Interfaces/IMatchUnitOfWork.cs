namespace Comeback.Match.Application.Common.Interfaces;

public interface IMatchUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
