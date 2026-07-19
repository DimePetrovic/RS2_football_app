namespace Comeback.Chat.Application.Common.Interfaces;
public interface IChatUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
