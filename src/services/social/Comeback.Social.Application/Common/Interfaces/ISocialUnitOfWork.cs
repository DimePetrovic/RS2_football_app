namespace Comeback.Social.Application.Common.Interfaces;

public interface ISocialUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
