using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Authorization;
using ProjectK.API.Extensions;
using ProjectK.API.Helpers;
using ProjectK.API.Models.Requests;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Project;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos.ProbesAndBadgesModule.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.API.Controllers.ProbesAndBadgesModule;

/// <summary>
/// The projects of a member's third probe: each described in Markdown with a few small pictures.
/// </summary>
/// <remarks>
/// Reading takes the right to read the member's probes. Writing is for the member themself and for
/// whoever the kurin lets sign their probe; the handlers check that, since no single permission
/// covers both.
/// </remarks>
[ApiController]
[Route("api/member/{memberKey:guid}/probes/{probeId}/projects")]
public class ProbeProjectController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProbeProjectController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the projects, whether the viewer may write them, and the limits.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpGet]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, "route:memberKey", ResourceType.Member)]
    [ProducesResponseType(typeof(ProbeProjectsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects(Guid memberKey, string probeId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetProbeProjectsQuery(memberKey, probeId), cancellationToken);
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Adds a project.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPost]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, "route:memberKey", ResourceType.Member)]
    [ProducesResponseType(typeof(ProbeProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddProject(
        Guid memberKey,
        string probeId,
        [FromBody] SaveProbeProjectRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(Command(memberKey, probeId, null, request), cancellationToken);
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Rewrites a project. Pictures the new description leaves out are deleted.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPut("{projectKey:guid}")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, "route:memberKey", ResourceType.Member)]
    [ProducesResponseType(typeof(ProbeProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProject(
        Guid memberKey,
        string probeId,
        Guid projectKey,
        [FromBody] SaveProbeProjectRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(Command(memberKey, probeId, projectKey, request), cancellationToken);
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Removes a project and its pictures.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpDelete("{projectKey:guid}")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, "route:memberKey", ResourceType.Member)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid memberKey, string probeId, Guid projectKey, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new DeleteProbeProjectCommand(memberKey, probeId, projectKey), cancellationToken);
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Uploads a picture for a project's description: PNG, JPEG or WebP, 1 MB at most, five per project.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPost("{projectKey:guid}/images")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, "route:memberKey", ResourceType.Member)]
    [RequestSizeLimit(ProbeProjectRules.MaxImageBytes + 64 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProbeProjectImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(
        Guid memberKey,
        string probeId,
        Guid projectKey,
        [FromForm] UploadImageRequest form,
        CancellationToken cancellationToken)
    {
        var file = form.File;
        if (file == null || file.Length == 0)
        {
            return this.Failure(ResultType.BadRequest, "MissingImage", "Image file is required.");
        }

        if (ImageUploadRules.Refusal(file, ProbeProjectRules.MaxImageBytes) is { } refusal)
        {
            return this.Failure(ResultType.BadRequest, refusal.Code, refusal.Message);
        }

        var bytes = await file.ToByteArrayAsync(cancellationToken);
        if (bytes == null || bytes.Length == 0)
        {
            return this.Failure(ResultType.BadRequest, "MissingImage", "Image file is required.");
        }

        var command = new UploadProbeProjectImageCommand(memberKey, probeId, projectKey, bytes, file.FileName);
        var response = await _mediator.Send(command, cancellationToken);
        return response.ToActionResult(this);
    }

    private static SaveProbeProjectCommand Command(Guid memberKey, string probeId, Guid? projectKey, SaveProbeProjectRequest request) =>
        new(
            memberKey,
            probeId,
            projectKey,
            request.Title,
            request.IsIndividual,
            request.Description,
            request.DirectionMentorName,
            request.DirectionMentorContact);
}
