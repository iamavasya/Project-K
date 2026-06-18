using MediatR;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications
{
    public sealed record GetNotifications(bool UnreadOnly, int Take) : IRequest<ServiceResult<IReadOnlyList<AppNotificationDto>>>;

    public sealed record GetUnreadNotificationCount : IRequest<ServiceResult<int>>;

    public sealed record MarkNotificationAsRead(Guid NotificationKey) : IRequest<ServiceResult<AppNotificationDto>>;

    public sealed record MarkAllNotificationsAsRead : IRequest<ServiceResult<int>>;

    public sealed class GetNotificationsHandler
        : IRequestHandler<GetNotifications, ServiceResult<IReadOnlyList<AppNotificationDto>>>
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserContext _currentUserContext;

        public GetNotificationsHandler(
            INotificationService notificationService,
            ICurrentUserContext currentUserContext)
        {
            _notificationService = notificationService;
            _currentUserContext = currentUserContext;
        }

        public async Task<ServiceResult<IReadOnlyList<AppNotificationDto>>> Handle(
            GetNotifications request,
            CancellationToken cancellationToken)
        {
            if (!_currentUserContext.UserId.HasValue)
            {
                return new ServiceResult<IReadOnlyList<AppNotificationDto>>(ResultType.Unauthorized);
            }

            var inbox = await _notificationService.GetInboxAsync(
                new NotificationQuery
                {
                    UnreadOnly = request.UnreadOnly,
                    Take = request.Take
                },
                cancellationToken);

            return new ServiceResult<IReadOnlyList<AppNotificationDto>>(ResultType.Success, inbox);
        }
    }

    public sealed class GetUnreadNotificationCountHandler
        : IRequestHandler<GetUnreadNotificationCount, ServiceResult<int>>
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserContext _currentUserContext;

        public GetUnreadNotificationCountHandler(
            INotificationService notificationService,
            ICurrentUserContext currentUserContext)
        {
            _notificationService = notificationService;
            _currentUserContext = currentUserContext;
        }

        public async Task<ServiceResult<int>> Handle(
            GetUnreadNotificationCount request,
            CancellationToken cancellationToken)
        {
            if (!_currentUserContext.UserId.HasValue)
            {
                return new ServiceResult<int>(ResultType.Unauthorized);
            }

            var count = await _notificationService.GetUnreadCountAsync(cancellationToken);
            return new ServiceResult<int>(ResultType.Success, count);
        }
    }

    public sealed class MarkNotificationAsReadHandler
        : IRequestHandler<MarkNotificationAsRead, ServiceResult<AppNotificationDto>>
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserContext _currentUserContext;

        public MarkNotificationAsReadHandler(
            INotificationService notificationService,
            ICurrentUserContext currentUserContext)
        {
            _notificationService = notificationService;
            _currentUserContext = currentUserContext;
        }

        public async Task<ServiceResult<AppNotificationDto>> Handle(
            MarkNotificationAsRead request,
            CancellationToken cancellationToken)
        {
            if (!_currentUserContext.UserId.HasValue)
            {
                return new ServiceResult<AppNotificationDto>(ResultType.Unauthorized);
            }

            var notification = await _notificationService.MarkAsReadAsync(
                request.NotificationKey,
                cancellationToken);

            return notification is null
                ? new ServiceResult<AppNotificationDto>(ResultType.NotFound)
                : new ServiceResult<AppNotificationDto>(ResultType.Success, notification);
        }
    }

    public sealed class MarkAllNotificationsAsReadHandler
        : IRequestHandler<MarkAllNotificationsAsRead, ServiceResult<int>>
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserContext _currentUserContext;

        public MarkAllNotificationsAsReadHandler(
            INotificationService notificationService,
            ICurrentUserContext currentUserContext)
        {
            _notificationService = notificationService;
            _currentUserContext = currentUserContext;
        }

        public async Task<ServiceResult<int>> Handle(
            MarkAllNotificationsAsRead request,
            CancellationToken cancellationToken)
        {
            if (!_currentUserContext.UserId.HasValue)
            {
                return new ServiceResult<int>(ResultType.Unauthorized);
            }

            var count = await _notificationService.MarkAllAsReadAsync(cancellationToken);
            return new ServiceResult<int>(ResultType.Success, count);
        }
    }
}
