using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Authorization;
using ProjectK.API.Extensions;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.DuesModule.Features.Kurin.SetRate;
using ProjectK.Common.Models.Dtos.DuesModule.Requests;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Controllers.DuesModule;

/// <summary>The kurin's side of the вкладка: the rates every гурток charges by.</summary>
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

    [HttpPut("rate")]
    [ResourceAuthorize(ResourceType.KurinDues, ResourceAction.Update, "route:kurinKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetRate(Guid kurinKey, [FromBody] SetKurinDuesRateRequest request)
    {
        var result = await _mediator.Send(new SetKurinDuesRateCommand(kurinKey, request));
        return result.ToActionResult(this);
    }
}
