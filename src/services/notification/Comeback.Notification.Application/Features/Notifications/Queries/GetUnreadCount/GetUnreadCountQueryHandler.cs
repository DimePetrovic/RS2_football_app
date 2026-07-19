namespace Comeback.Notification.Application.Features.Notifications.Queries.GetUnreadCount;

using Comeback.Notification.Application.Common.Interfaces;
using MediatR;

internal sealed class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly IInAppNotificationRepository _repository;

    public GetUnreadCountQueryHandler(IInAppNotificationRepository repository)
        => _repository = repository;

    public Task<int> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken)
        => _repository.GetUnreadCountAsync(query.UserId, cancellationToken);
}
