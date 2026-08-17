using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Categories;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Create;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Responses;
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

        // ---- Event groups (categories) ----

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{kurinKey:guid}/categories")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<AgendaCategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetAgendaCategories(kurinKey, IncludeArchived: false));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{kurinKey:guid}/categories/manage")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Update, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<AgendaCategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategoriesForManagement(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetAgendaCategories(kurinKey, IncludeArchived: true));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPost("categories")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Update, "arg:request.KurinKey")]
        [ProducesResponseType(typeof(AgendaCategoryResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpsertCategory([FromBody] UpsertAgendaCategory request)
        {
            var response = await _mediator.Send(request with { AgendaCategoryKey = null });
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPut("categories/{categoryKey:guid}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Update, "arg:request.KurinKey")]
        [ProducesResponseType(typeof(AgendaCategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(Guid categoryKey, [FromBody] UpsertAgendaCategory request)
        {
            // The route key wins so a mismatched body cannot retarget another group.
            var response = await _mediator.Send(request with { AgendaCategoryKey = categoryKey });
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpDelete("{kurinKey:guid}/categories/{categoryKey:guid}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Update, "route:kurinKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCategory(Guid kurinKey, Guid categoryKey)
        {
            var response = await _mediator.Send(new DeleteAgendaCategory(categoryKey, kurinKey));
            return response.ToActionResult(this);
        }

        // ---- RSVP ----

        [Authorize(Policy = "RequireUser")]
        [HttpGet("{agendaItemKey:guid}/responses")]
        [ProducesResponseType(typeof(AgendaResponsesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResponses(Guid agendaItemKey)
        {
            var response = await _mediator.Send(new GetAgendaResponses(agendaItemKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPut("{agendaItemKey:guid}/response")]
        [ProducesResponseType(typeof(AgendaResponsesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetResponse(Guid agendaItemKey, [FromBody] SetAgendaResponseRequest request)
        {
            var response = await _mediator.Send(new SetAgendaResponse(agendaItemKey, request.Status));
            return response.ToActionResult(this);
        }
    }

    /// <summary>Body for the board status move.</summary>
    public sealed record ChangeAgendaStatusRequest(AgendaItemStatus Status);

    /// <summary>Body for an RSVP set.</summary>
    public sealed record SetAgendaResponseRequest(AgendaRsvpStatus Status);
}
