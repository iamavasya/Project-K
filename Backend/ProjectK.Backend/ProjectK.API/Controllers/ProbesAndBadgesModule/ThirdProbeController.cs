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
/// A member's third probe: the file (opening, mentors, conference, camp and its посвідка, résumé)
/// and the projects, each laid out as the УПЮ template asks, with small inline pictures.
/// </summary>
/// <remarks>
/// Reading takes the right to read the member's probes. Writing is for the member themself and for
/// whoever the kurin lets sign their probe; confirming takes that signing right, taking a
/// confirmation back the unsigning one. The handlers check these, since no single permission covers
/// the person and their leaders at once. Every write answers with the whole tab.
/// </remarks>
[ApiController]
[Route("api/member/{memberKey:guid}/probes/{probeId}")]
public class ThirdProbeController : ControllerBase
{
    private const string ReadProbes = "route:memberKey";
    private readonly IMediator _mediator;

    public ThirdProbeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the third-probe tab: file, projects, вмілості, what the viewer may do, and the limits.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpGet("third-probe")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThirdProbe(Guid memberKey, string probeId, CancellationToken cancellationToken) =>
        (await _mediator.Send(new GetThirdProbeQuery(memberKey, probeId), cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Writes the file: opening date, mentors, conference, camp, the вмілість for тіловиховання, résumé.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPut("third-probe")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SaveFile(
        Guid memberKey,
        string probeId,
        [FromBody] SaveThirdProbeFileRequest request,
        CancellationToken cancellationToken)
    {
        var input = new ThirdProbeFileInput(
            request.OpenedOn, request.Mentors, request.ConferenceOn, request.ConferenceWith, request.CampKind,
            request.CampName, request.CampFrom, request.CampTo, request.PhysicalBadgeId, request.Resume);
        return (await _mediator.Send(new SaveThirdProbeFileCommand(memberKey, probeId, input), cancellationToken)).ToActionResult(this);
    }

    /// <summary>
    /// Confirms the opening conference.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPut("third-probe/conference/confirmation")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> ConfirmConference(Guid memberKey, string probeId, CancellationToken cancellationToken) =>
        SignAsync(memberKey, probeId, ThirdProbeSignPart.Conference, true, null, null, cancellationToken);

    /// <summary>
    /// Takes the conference's confirmation back.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpDelete("third-probe/conference/confirmation")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> UnconfirmConference(Guid memberKey, string probeId, CancellationToken cancellationToken) =>
        SignAsync(memberKey, probeId, ThirdProbeSignPart.Conference, false, null, null, cancellationToken);

    /// <summary>
    /// Counts the camp for the probe.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPut("third-probe/camp/confirmation")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> ConfirmCamp(Guid memberKey, string probeId, CancellationToken cancellationToken) =>
        SignAsync(memberKey, probeId, ThirdProbeSignPart.Camp, true, null, null, cancellationToken);

    /// <summary>
    /// Takes the camp's confirmation back.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpDelete("third-probe/camp/confirmation")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> UnconfirmCamp(Guid memberKey, string probeId, CancellationToken cancellationToken) =>
        SignAsync(memberKey, probeId, ThirdProbeSignPart.Camp, false, null, null, cancellationToken);

    /// <summary>
    /// Attaches the camp's посвідка: a picture (PNG, JPEG, WebP) or a PDF, 2 MB at most.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPost("third-probe/camp/certificate")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [RequestSizeLimit(ProbeProjectRules.MaxCertificateBytes + 64 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadCertificate(
        Guid memberKey,
        string probeId,
        [FromForm] UploadImageRequest form,
        CancellationToken cancellationToken)
    {
        var file = form.File;
        if (file == null || file.Length == 0)
        {
            return this.Failure(ResultType.BadRequest, "MissingFile", "A file is required.");
        }

        var isPdf = string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        if (file.Length > ProbeProjectRules.MaxCertificateBytes)
        {
            return this.Failure(ResultType.BadRequest, "CertificateTooLarge", "The посвідка must be 2 MB or smaller.");
        }

        if (!isPdf && ImageUploadRules.Refusal(file, ProbeProjectRules.MaxCertificateBytes) is { } refusal)
        {
            return this.Failure(ResultType.BadRequest, "InvalidCertificate", refusal.Message);
        }

        var bytes = await file.ToByteArrayAsync(cancellationToken);
        if (bytes == null || bytes.Length == 0)
        {
            return this.Failure(ResultType.BadRequest, "MissingFile", "A file is required.");
        }

        var command = new UploadCampCertificateCommand(memberKey, probeId, bytes, file.FileName, isPdf);
        return (await _mediator.Send(command, cancellationToken)).ToActionResult(this);
    }

    /// <summary>
    /// Takes the посвідка off.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpDelete("third-probe/camp/certificate")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCertificate(Guid memberKey, string probeId, CancellationToken cancellationToken) =>
        (await _mediator.Send(new DeleteCampCertificateCommand(memberKey, probeId), cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Adds a project: a main one while there is room for five, then the spare.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPost("projects")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ProbeProjectSavedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddProject(
        Guid memberKey,
        string probeId,
        [FromBody] SaveProbeProjectRequest request,
        CancellationToken cancellationToken) =>
        (await _mediator.Send(new SaveProbeProjectCommand(memberKey, probeId, null, Input(request)), cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Rewrites a project. Pictures the new texts leave out are deleted.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPut("projects/{projectKey:guid}")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ProbeProjectSavedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProject(
        Guid memberKey,
        string probeId,
        Guid projectKey,
        [FromBody] SaveProbeProjectRequest request,
        CancellationToken cancellationToken) =>
        (await _mediator.Send(new SaveProbeProjectCommand(memberKey, probeId, projectKey, Input(request)), cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Removes a project and its pictures; the spare takes a freed main place.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpDelete("projects/{projectKey:guid}")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid memberKey, string probeId, Guid projectKey, CancellationToken cancellationToken) =>
        (await _mediator.Send(new DeleteProbeProjectCommand(memberKey, probeId, projectKey), cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Puts the spare project in a main one's place; the main one becomes the spare.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPost("projects/{spareKey:guid}/swap/{mainKey:guid}")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SwapSpare(Guid memberKey, string probeId, Guid spareKey, Guid mainKey, CancellationToken cancellationToken) =>
        (await _mediator.Send(new SwapSpareProjectCommand(memberKey, probeId, spareKey, mainKey), cancellationToken)).ToActionResult(this);

    /// <summary>
    /// «Дозвіл»: the project may go ahead.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPut("projects/{projectKey:guid}/approval")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ApproveProject(Guid memberKey, string probeId, Guid projectKey, CancellationToken cancellationToken) =>
        SignAsync(memberKey, probeId, ThirdProbeSignPart.ProjectApproval, true, projectKey, null, cancellationToken);

    /// <summary>
    /// Takes the permission back.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpDelete("projects/{projectKey:guid}/approval")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UnapproveProject(Guid memberKey, string probeId, Guid projectKey, CancellationToken cancellationToken) =>
        SignAsync(memberKey, probeId, ThirdProbeSignPart.ProjectApproval, false, projectKey, null, cancellationToken);

    /// <summary>
    /// «Успішно виконав»: the project is done, with what its coordinator said.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPut("projects/{projectKey:guid}/completion")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> CompleteProject(
        Guid memberKey,
        string probeId,
        Guid projectKey,
        [FromBody] CompleteProbeProjectRequest? request,
        CancellationToken cancellationToken) =>
        SignAsync(memberKey, probeId, ThirdProbeSignPart.ProjectCompletion, true, projectKey, request?.CoordinatorComment, cancellationToken);

    /// <summary>
    /// Takes the completion back.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpDelete("projects/{projectKey:guid}/completion")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
    [ProducesResponseType(typeof(ThirdProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UncompleteProject(Guid memberKey, string probeId, Guid projectKey, CancellationToken cancellationToken) =>
        SignAsync(memberKey, probeId, ThirdProbeSignPart.ProjectCompletion, false, projectKey, null, cancellationToken);

    /// <summary>
    /// Uploads a picture for a project's texts: PNG, JPEG or WebP, 1 MB at most, five per project.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireUser)]
    [HttpPost("projects/{projectKey:guid}/images")]
    [ResourceAuthorize(ResourceType.ProbeProgress, ResourceAction.Read, ReadProbes, ResourceType.Member)]
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
        return (await _mediator.Send(command, cancellationToken)).ToActionResult(this);
    }

    private async Task<IActionResult> SignAsync(
        Guid memberKey,
        string probeId,
        ThirdProbeSignPart part,
        bool sign,
        Guid? projectKey,
        string? comment,
        CancellationToken cancellationToken) =>
        (await _mediator.Send(new SignThirdProbeCommand(memberKey, probeId, part, sign, projectKey, comment), cancellationToken)).ToActionResult(this);

    private static ProbeProjectInput Input(SaveProbeProjectRequest request) => new(
        request.Title,
        request.Area,
        request.Reach,
        request.IsShared,
        request.SharedWith,
        request.StartsOn,
        request.EndsOn,
        request.Location,
        request.EventLeads,
        request.CoordinatorName,
        request.CoordinatorContact,
        request.Goal,
        request.Plan,
        request.Results);
}
