using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Authorization;
using ProjectK.API.Extensions;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.MeModule.Features.Events;
using ProjectK.BusinessLogic.Modules.MeModule.Features.Tasks;
using ProjectK.BusinessLogic.Modules.MeModule.Models;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Controllers.MeModule;

/// <summary>
/// The person behind the token, across every kurin they stand in — what the dashboard reads. No
/// key in any route: there is nobody else to ask about, so there is nothing to authorize beyond
/// being signed in. What each kurin lets them see is settled per kurin inside.
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize(Policy = AuthorizationPolicies.RequireUser)]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Events ahead in the next <paramref name="days"/> (14 by default), with the person's own answers.</summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(IReadOnlyList<MyEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] int days = 14)
    {
        var result = await _mediator.Send(new GetMyEventsQuery(days));
        return result.ToActionResult(this);
    }

    [HttpPut("events/{agendaItemKey:guid}/response")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetEventResponse(Guid agendaItemKey, [FromBody] SetMyEventResponseRequest request)
    {
        var result = await _mediator.Send(new SetMyEventResponseCommand(agendaItemKey, request.Status ?? AgendaRsvpStatus.Going));
        return result.ToActionResult(this);
    }

    /// <summary>Open tasks the person is on the hook for, or raised, in every kurin.</summary>
    [HttpGet("tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<MyTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks()
    {
        var result = await _mediator.Send(new GetMyTasksQuery());
        return result.ToActionResult(this);
    }
}

public sealed record SetMyEventResponseRequest(AgendaRsvpStatus? Status);
