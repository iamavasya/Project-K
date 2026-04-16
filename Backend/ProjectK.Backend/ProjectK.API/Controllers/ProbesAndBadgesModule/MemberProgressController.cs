using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Get;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Review;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Submit;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.Get;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.UpdatePointSignature;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.UpdateStatus;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Controllers.ProbesAndBadgesModule;

[ApiController]
[Route("api/member/{memberKey:guid}")]
public class MemberProgressController : ControllerBase
{
    private readonly IMediator _mediator;

    public MemberProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "RequireUser")]
    [HttpGet("badges/progress")]
    [ResourceAuthorize(ResourceType.Member, ResourceAction.Read, "route:memberKey")]
    [ProducesResponseType(typeof(IEnumerable<BadgeProgressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBadgeProgresses(Guid memberKey)
    {
        var response = await _mediator.Send(new GetBadgeProgresses(memberKey));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireUser")]
    [HttpPost("badges/{badgeId}/submit")]
    [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
    [ProducesResponseType(typeof(BadgeProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitBadgeProgress(Guid memberKey, string badgeId, [FromBody] SubmitBadgeProgressRequest request)
    {
        var response = await _mediator.Send(new SubmitBadgeProgress(memberKey, badgeId, request?.Note));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireMentor")]
    [HttpPost("badges/{badgeId}/review")]
    [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
    [ProducesResponseType(typeof(BadgeProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReviewBadgeProgress(Guid memberKey, string badgeId, [FromBody] ReviewBadgeProgressRequest request)
    {
        var response = await _mediator.Send(new ReviewBadgeProgress(memberKey, badgeId, request.IsApproved, request.Note));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireUser")]
    [HttpGet("probes/{probeId}/progress")]
    [ResourceAuthorize(ResourceType.Member, ResourceAction.Read, "route:memberKey")]
    [ProducesResponseType(typeof(ProbeProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProbeProgress(Guid memberKey, string probeId)
    {
        var response = await _mediator.Send(new GetProbeProgress(memberKey, probeId));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireMentor")]
    [HttpPut("probes/{probeId}/progress/status")]
    [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
    [ProducesResponseType(typeof(ProbeProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProbeProgressStatus(
        Guid memberKey,
        string probeId,
        [FromBody] UpdateProbeProgressStatusRequest request)
    {
        var response = await _mediator.Send(new UpdateProbeProgressStatus(memberKey, probeId, request.Status, request.Note));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireMentor")]
    [HttpPut("probes/{probeId}/points/{pointId}/sign")]
    [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
    [ProducesResponseType(typeof(ProbeProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignProbePoint(
        Guid memberKey,
        string probeId,
        string pointId,
        [FromBody] UpdateProbePointSignatureRequest? request)
    {
        var response = await _mediator.Send(new UpdateProbePointSignature(memberKey, probeId, pointId, true, request?.Note));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireMentor")]
    [HttpPut("probes/{probeId}/points/{pointId}/unsign")]
    [ResourceAuthorize(ResourceType.Member, ResourceAction.Update, "route:memberKey")]
    [ProducesResponseType(typeof(ProbeProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnsignProbePoint(
        Guid memberKey,
        string probeId,
        string pointId,
        [FromBody] UpdateProbePointSignatureRequest? request)
    {
        var response = await _mediator.Send(new UpdateProbePointSignature(memberKey, probeId, pointId, false, request?.Note));
        return response.ToActionResult(this);
    }
}
