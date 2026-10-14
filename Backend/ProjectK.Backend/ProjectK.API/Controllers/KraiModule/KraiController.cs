using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Authorization;
using ProjectK.API.Extensions;
using ProjectK.BusinessLogic.Modules.KraiModule.Features.List;
using ProjectK.BusinessLogic.Modules.KraiModule.Models;

namespace ProjectK.API.Controllers.KraiModule;

/// <summary>The краї of this instance. Every kurin belongs to one.</summary>
[Route("api/krai")]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
[ApiController]
public class KraiController : ControllerBase
{
    private readonly IMediator _mediator;

    public KraiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lists the краї with how many kurins each holds.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<KraiResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var response = await _mediator.Send(new GetKraisQuery());
        return response.ToActionResult(this);
    }
}
