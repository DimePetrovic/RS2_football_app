namespace Comeback.Notification.Application.Common.Interfaces;

public interface IAllUsersClient
{
    /// <summary>All registered user ids — used to broadcast a public call.</summary>
    Task<IReadOnlyList<Guid>> GetAllUserIdsAsync(CancellationToken ct = default);
}
