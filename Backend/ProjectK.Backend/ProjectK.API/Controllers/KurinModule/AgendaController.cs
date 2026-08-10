using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Create;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Status;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Update;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Controllers.KurinModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgendaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AgendaController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{kurinKey:guid}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<AgendaItemResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCalendar(Guid kurinKey, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc)
        {
            var response = await _mediator.Send(new GetAgendaItems(kurinKey, fromUtc, toUtc));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{kurinKey:guid}/board")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<AgendaItemResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBoard(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetAgendaBoard(kurinKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpGet("{kurinKey:guid}/assign-targets")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(AgendaAssignTargetsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignTargets(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetAssignTargets(kurinKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpPost]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "arg:request.KurinKey")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateAgendaItem request)
        {
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpPut("{agendaItemKey:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid agendaItemKey, [FromBody] UpdateAgendaItem request)
        {
            // The route key wins so a mismatched body cannot retarget another item.
            var response = await _mediator.Send(request with { AgendaItemKey = agendaItemKey });
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPut("{agendaItemKey:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeStatus(Guid agendaItemKey, [FromBody] ChangeAgendaStatusRequest request)
        {
            var response = await _mediator.Send(new ChangeAgendaItemStatus(agendaItemKey, request.Status));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireMentor")]
        [HttpDelete("{agendaItemKey:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid agendaItemKey)
        {
            var response = await _mediator.Send(new DeleteAgendaItem(agendaItemKey));
            return response.ToActionResult(this);
        }
    }

    /// <summary>Body for the board status move.</summary>
    public sealed record ChangeAgendaStatusRequest(AgendaItemStatus Status);
}
