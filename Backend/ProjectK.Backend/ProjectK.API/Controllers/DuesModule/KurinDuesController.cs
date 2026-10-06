using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Authorization;
using ProjectK.API.Extensions;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.DuesModule.Features.Kurin.Entry;
using ProjectK.BusinessLogic.Modules.DuesModule.Features.Kurin.Get;
using ProjectK.BusinessLogic.Modules.DuesModule.Features.Kurin.Receive;
using ProjectK.BusinessLogic.Modules.DuesModule.Features.Kurin.SetRate;
using ProjectK.BusinessLogic.Modules.DuesModule.Models;
using ProjectK.Common.Models.Dtos.DuesModule.Requests;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Controllers.DuesModule;

/// <summary>
/// The kurin's side of the вкладка: the rates every гурток charges by, the kurin's own box, and the
/// transfers гуртки hand up. Every route is keyed by the kurin, and the access check reads the scope
/// from it.
/// </summary>
[ApiController]
[Route("api/kurin/{kurinKey:guid}/dues")]
[Authorize(Policy = AuthorizationPolicies.RequireUser)]
public class KurinDuesController : ControllerBase
{
    private readonly IMediator _mediator;

    public KurinDuesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ResourceAuthorize(ResourceType.KurinDues, ResourceAction.Read, "route:kurinKey")]
    [ProducesResponseType(typeof(KurinDuesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid kurinKey)
    {
        var result = await _mediator.Send(new GetKurinDuesQuery(kurinKey));
        return result.ToActionResult(this);
    }

    [HttpPut("rate")]
    [ResourceAuthorize(ResourceType.KurinDues, ResourceAction.Update, "route:kurinKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetRate(Guid kurinKey, [FromBody] SetKurinDuesRateRequest request)
    {
        var result = await _mediator.Send(new SetKurinDuesRateCommand(kurinKey, request));
        return result.ToActionResult(this);
    }

    [HttpPut("transfers/{entryKey:guid}/received")]
    [ResourceAuthorize(ResourceType.KurinDues, ResourceAction.Update, "route:kurinKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTransferReceived(Guid kurinKey, Guid entryKey, [FromBody] SetDuesTransferReceivedRequest request)
    {
        var result = await _mediator.Send(new SetDuesTransferReceivedCommand(kurinKey, entryKey, request.IsReceived));
        return result.ToActionResult(this);
    }

    [HttpPost("entries")]
    [ResourceAuthorize(ResourceType.KurinDues, ResourceAction.Create, "route:kurinKey")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEntry(Guid kurinKey, [FromBody] UpsertDuesEntryRequest request)
    {
        var result = await _mediator.Send(new CreateKurinDuesEntryCommand(kurinKey, request));
        return result.ToActionResult(this);
    }

    [HttpPut("entries/{entryKey:guid}")]
    [ResourceAuthorize(ResourceType.KurinDues, ResourceAction.Update, "route:kurinKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEntry(Guid kurinKey, Guid entryKey, [FromBody] UpsertDuesEntryRequest request)
    {
        var result = await _mediator.Send(new UpdateKurinDuesEntryCommand(kurinKey, entryKey, request));
        return result.ToActionResult(this);
    }

    [HttpDelete("entries/{entryKey:guid}")]
    [ResourceAuthorize(ResourceType.KurinDues, ResourceAction.Delete, "route:kurinKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEntry(Guid kurinKey, Guid entryKey)
    {
        var result = await _mediator.Send(new DeleteKurinDuesEntryCommand(kurinKey, entryKey));
        return result.ToActionResult(this);
    }

    [HttpPut("entries/{entryKey:guid}/verified")]
    [ResourceAuthorize(ResourceType.KurinDues, ResourceAction.Manage, "route:kurinKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetEntryVerified(Guid kurinKey, Guid entryKey, [FromBody] SetDuesEntryVerifiedRequest request)
    {
        var result = await _mediator.Send(new SetKurinDuesEntryVerifiedCommand(kurinKey, entryKey, request.IsVerified));
        return result.ToActionResult(this);
    }
}
