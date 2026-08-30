using MediatR;
using ProjectK.API.Extensions;
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
using ProjectK.API.Authorization;

namespace ProjectK.API.Controllers.KurinModule
{
    /// <summary>
    /// The kurin's agenda: items, the categories they are filed under, and who has answered that they are
    /// coming. What a caller sees is decided by the assignments on each item, not by the item's author.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgendaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AgendaController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns agenda items in a date range, narrowed to what the caller may see.
        /// </summary>
        /// <remarks>
        /// Whole-kurin viewers see everything; everyone else sees only items assigned to them, their groups or
        /// their offices.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("{kurinKey:guid}")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<AgendaItemResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCalendar(Guid kurinKey, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc)
        {
            var response = await _mediator.Send(new GetAgendaItems(kurinKey, fromUtc, toUtc));
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Returns the agenda grouped for the board view rather than by date.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("{kurinKey:guid}/board")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<AgendaItemResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBoard(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetAgendaBoard(kurinKey));
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Lists what an item can be assigned to — the kurin, its groups, its offices and its members.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireAgendaAuthor)]
        [HttpGet("{kurinKey:guid}/assign-targets")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(AgendaAssignTargetsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignTargets(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetAssignTargets(kurinKey));
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Creates an agenda item together with its assignments.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireAgendaAuthor)]
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

        /// <summary>
        /// Rewrites an agenda item.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpPut("{agendaItemKey:guid}")]
        [ResourceAuthorize(ResourceType.AgendaItem, ResourceAction.Update, "route:agendaItemKey")]
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

        /// <summary>
        /// Moves an item between statuses.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Update"/> because who may change a status is not who may rewrite the item.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpPut("{agendaItemKey:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeStatus(Guid agendaItemKey, [FromBody] ChangeAgendaStatusRequest request)
        {
            var response = await _mediator.Send(new ChangeAgendaItemStatus(agendaItemKey, request.Status));
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Deletes an agenda item.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpDelete("{agendaItemKey:guid}")]
        [ResourceAuthorize(ResourceType.AgendaItem, ResourceAction.Delete, "route:agendaItemKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid agendaItemKey)
        {
            var response = await _mediator.Send(new DeleteAgendaItem(agendaItemKey));
            return response.ToActionResult(this);
        }

        // ---- Event groups (categories) ----

        /// <summary>
        /// Lists the categories available when filing an item.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("{kurinKey:guid}/categories")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<AgendaCategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetAgendaCategories(kurinKey, IncludeArchived: false));
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Lists categories with the detail needed to edit them, which requires rights over the kurin.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("{kurinKey:guid}/categories/manage")]
        [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Update, "route:kurinKey")]
        [ProducesResponseType(typeof(IEnumerable<AgendaCategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategoriesForManagement(Guid kurinKey)
        {
            var response = await _mediator.Send(new GetAgendaCategories(kurinKey, IncludeArchived: true));
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Creates a category, or rewrites one matched by key.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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

        /// <summary>
        /// Rewrites one category.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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

        /// <summary>
        /// Deletes a category.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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

        /// <summary>
        /// Returns who has answered an item and how.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpGet("{agendaItemKey:guid}/responses")]
        [ProducesResponseType(typeof(AgendaResponsesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResponses(Guid agendaItemKey)
        {
            var response = await _mediator.Send(new GetAgendaResponses(agendaItemKey));
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Records the caller's own answer to an item.
        /// </summary>
        /// <remarks>
        /// Refused on items the caller cannot see, using the same visibility rule as the feed — the two are one
        /// definition, so the list and what may be answered cannot drift apart.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
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
