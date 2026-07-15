using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos;

namespace ProjectK.API.Controllers.InfrastructureModule
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<AppNotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetInbox(
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int take = 50,
            CancellationToken cancellationToken = default)
        {
            var response = await _mediator.Send(
                new GetNotifications(unreadOnly, take),
                cancellationToken);

            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new GetUnreadNotificationCount(), cancellationToken);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPut("{notificationKey:guid}/read")]
        [ProducesResponseType(typeof(AppNotificationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(
            Guid notificationKey,
            CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(
                new MarkNotificationAsRead(notificationKey),
                cancellationToken);

            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPut("read-all")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new MarkAllNotificationsAsRead(), cancellationToken);
            return response.ToActionResult(this);
        }
    }
}
