using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Create;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Get;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Controllers.KurinModule;

[Route("api/[controller]")]
[ApiController]
public class PlanningController : ControllerBase
{
    private readonly IMediator _mediator;
    public PlanningController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "RequireManager")]
    [HttpPost]
    [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Create, "arg:request.KurinKey")]
    public async Task<IActionResult> CreatePlanningSession([FromBody] CreatePlanningSession request)
    {
        var response = await _mediator.Send(request);
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireManager")]
    [HttpGet("session/{planningSessionKey:guid}")]
    [ResourceAuthorize(ResourceType.PlanningSession, ResourceAction.Read, "route:planningSessionKey")]
    public async Task<IActionResult> GetPlanningSessionByKey(Guid planningSessionKey)
    {
        var request = new GetPlanningSessionByKey(planningSessionKey);
        var response = await _mediator.Send(request);
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireManager")]
    [HttpGet("{kurinKey:guid}")]
    [ResourceAuthorize(ResourceType.Kurin, ResourceAction.Read, "route:kurinKey")]
    public async Task<IActionResult> GetPlanningSessions(Guid kurinKey)
    {
        var request = new GetPlanningSessions(kurinKey);
        var response = await _mediator.Send(request);
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireManager")]
    [HttpDelete("{planningSessionKey:guid}")]
    [ResourceAuthorize(ResourceType.PlanningSession, ResourceAction.Delete, "route:planningSessionKey")]
    public async Task<IActionResult> DeletePlanningSession(Guid planningSessionKey)
    {
        var request = new DeletePlanningSession(planningSessionKey);
        var response = await _mediator.Send(request);
        return response.ToActionResult(this);
    }
}
