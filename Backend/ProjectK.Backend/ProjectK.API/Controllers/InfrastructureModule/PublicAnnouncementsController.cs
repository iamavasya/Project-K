using MediatR;
using ProjectK.API.Extensions;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectK.API.Authorization;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Infrastructure.Services.BlobStorageService;
using ProjectK.Infrastructure.Services.BlobStorageService.OrphanCleanup;
using ProjectK.Common.Models.Settings;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.CleanupStatus;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Create;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Get;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Preview;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Publish;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Transition;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Update;
using ProjectK.API.Models.Requests;

namespace ProjectK.API.Controllers.InfrastructureModule;

/// <summary>
/// Announcements published outside the app — drafted, reviewed, approved and only then published.
/// Administrators, or a service token for the bot that drafts them.
/// </summary>
[ApiController]
[Route("api/admin/public-announcements")]
[Authorize(Policy = AdminOrServiceTokenRequirement.PolicyName)]
public class PublicAnnouncementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicAnnouncementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lists drafts, optionally narrowed to one status.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PublicAnnouncementDraftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PublicAnnouncementStatus? status)
    {
        var response = await _mediator.Send(new GetPublicAnnouncementDraftsQuery(status));
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Returns one draft.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpGet("{draftKey:guid}", Name = "GetPublicAnnouncementDraftByKey")]
    [ProducesResponseType(typeof(PublicAnnouncementDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByKey(Guid draftKey)
    {
        var response = await _mediator.Send(new GetPublicAnnouncementDraftQuery(draftKey));
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Reports which uploaded images no draft references any more.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpGet("cleanup-status")]
    [ProducesResponseType(typeof(PublicAnnouncementCleanupStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCleanupStatus()
    {
        var response = await _mediator.Send(new GetPublicAnnouncementCleanupStatusQuery());
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Creates a draft.
    /// </summary>
    /// <remarks>
    /// The one action here that also accepts a service token rather than an administrator: it is how the
    /// announcement bot files what it has written for a human to review.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(PublicAnnouncementDraftDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePublicAnnouncementDraftRequestDto request)
    {
        var response = await _mediator.Send(new CreatePublicAnnouncementDraftCommand(
            request.Title,
            request.Body,
            request.SourceType,
            request.SourceId,
            request.SourceUrl,
            request.Environment,
            request.Version,
            request.Codename,
            request.ParseMode,
            request.ImageBlobKey,
            request.ImageUrl,
            request.ImageAltText,
            request.ImagePlacement,
            request.TemplateKey,
            request.TemplateDataJson));

        return response.ToActionResult(this);
    }

    /// <summary>
    /// Stores an image for use in a draft and returns its key and URL.
    /// </summary>
    /// <remarks>
    /// Capped at 8 MB and refused unless the content type is an image.
    /// </remarks>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpPost("image")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PublicAnnouncementImageUploadDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadImage(
        [FromForm] UploadImageRequest form,
        [FromServices] IPublicAnnouncementImageStore imageStore,
        CancellationToken cancellationToken)
    {
        var file = form.File;
        if (file == null || file.Length == 0)
        {
            return this.Failure(ResultType.BadRequest, "ImageRequired", "Image file is required.");
        }

        if (file.Length > 8 * 1024 * 1024)
        {
            return this.Failure(ResultType.BadRequest, "ImageTooLarge", "Image must be 8 MB or smaller.");
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return this.Failure(ResultType.BadRequest, "InvalidImageType", "Only image files are supported.");
        }

        var bytes = await file.ToByteArrayAsync(cancellationToken);
        if (bytes == null || bytes.Length == 0)
        {
            return this.Failure(ResultType.BadRequest, "ImageRequired", "Image file is required.");
        }

        try
        {
            var result = await imageStore.SaveAsync(bytes, file.FileName, file.ContentType, cancellationToken);
            var imageUrl = Url.ActionLink(
                action: nameof(GetImage),
                controller: null,
                values: new { imageKey = result.ImageKey });

            return Ok(new PublicAnnouncementImageUploadDto(result.ImageKey, imageUrl));
        }
        catch (InvalidOperationException)
        {
            return this.Failure(ResultType.BadRequest, "InvalidImageContent", "Uploaded file is not a valid image.");
        }
    }

    /// <summary>
    /// Serves a stored announcement image.
    /// </summary>
    /// <remarks>
    /// Anonymous, because a published announcement is read outside the app by people who have no account.
    /// </remarks>
    [HttpGet("image/{*imageKey}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/octet-stream")]
    public async Task<IActionResult> GetImage(
        string imageKey,
        [FromServices] IPublicAnnouncementImageStore imageStore,
        CancellationToken cancellationToken)
    {
        var image = await imageStore.OpenAsync(imageKey, cancellationToken);
        if (image == null)
        {
            return this.Failure(ResultType.NotFound, "ImageNotFound", "Announcement image was not found.");
        }

        return File(image.Content, image.ContentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Permanently removes a stored image.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpDelete("image/{*imageKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteImage(
        string imageKey,
        [FromServices] IPublicAnnouncementImageStore imageStore,
        CancellationToken cancellationToken)
    {
        await imageStore.DeleteAsync(imageKey, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Rewrites a draft that has not been published.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpPut("{draftKey:guid}")]
    [ProducesResponseType(typeof(PublicAnnouncementDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid draftKey, [FromBody] UpdatePublicAnnouncementDraftRequestDto request)
    {
        var response = await _mediator.Send(new UpdatePublicAnnouncementDraftCommand(
            draftKey,
            request.Title,
            request.Body,
            request.ParseMode,
            request.ImageBlobKey,
            request.ImageUrl,
            request.ImageAltText,
            request.ImagePlacement,
            request.TemplateKey,
            request.TemplateDataJson));

        return response.ToActionResult(this);
    }

    /// <summary>
    /// Renders the draft exactly as it would be published, without publishing it.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpPost("{draftKey:guid}/preview")]
    [ProducesResponseType(typeof(PublicAnnouncementPreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(Guid draftKey)
    {
        var response = await _mediator.Send(new PreviewPublicAnnouncementDraftQuery(draftKey));
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Moves a draft into review.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpPost("{draftKey:guid}/submit")]
    [ProducesResponseType(typeof(PublicAnnouncementDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitForApproval(Guid draftKey)
    {
        var response = await _mediator.Send(new TransitionPublicAnnouncementDraftCommand(
            draftKey,
            PublicAnnouncementStatus.PendingApproval));
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Approves a draft for publication. Approving does not publish it.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpPost("{draftKey:guid}/approve")]
    [ProducesResponseType(typeof(PublicAnnouncementDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid draftKey)
    {
        var response = await _mediator.Send(new TransitionPublicAnnouncementDraftCommand(
            draftKey,
            PublicAnnouncementStatus.Approved));
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Sends a draft back from review.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpPost("{draftKey:guid}/reject")]
    [ProducesResponseType(typeof(PublicAnnouncementDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(Guid draftKey)
    {
        var response = await _mediator.Send(new TransitionPublicAnnouncementDraftCommand(
            draftKey,
            PublicAnnouncementStatus.Rejected));
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Publishes an approved draft.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpPost("{draftKey:guid}/publish")]
    [ProducesResponseType(typeof(PublicAnnouncementDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(Guid draftKey)
    {
        var response = await _mediator.Send(new PublishPublicAnnouncementDraftCommand(draftKey));
        return response.ToActionResult(this);
    }

    /// <summary>
    /// Deletes a draft.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpDelete("{draftKey:guid}")]
    [ProducesResponseType(typeof(PublicAnnouncementDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid draftKey)
    {
        var response = await _mediator.Send(new TransitionPublicAnnouncementDraftCommand(
            draftKey,
            PublicAnnouncementStatus.Deleted));
        return response.ToActionResult(this);
    }
}
