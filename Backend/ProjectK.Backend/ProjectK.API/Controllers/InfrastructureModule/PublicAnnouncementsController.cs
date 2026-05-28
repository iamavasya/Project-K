using MediatR;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectK.API.Services.Authorization;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Commands;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Queries;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.Services.BlobStorageService;
using ProjectK.Infrastructure.Services.BlobStorageService.OrphanCleanup;

namespace ProjectK.API.Controllers.InfrastructureModule;

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

    [Authorize(Policy = "RequireAdmin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PublicAnnouncementStatus? status)
    {
        var response = await _mediator.Send(new GetPublicAnnouncementDraftsQuery(status));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpGet("{draftKey:guid}", Name = "GetPublicAnnouncementDraftByKey")]
    public async Task<IActionResult> GetByKey(Guid draftKey)
    {
        var response = await _mediator.Send(new GetPublicAnnouncementDraftQuery(draftKey));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpGet("cleanup-status")]
    public async Task<IActionResult> GetCleanupStatus(
        [FromServices] AppDbContext db,
        [FromServices] BlobStorageOptions blobOptions,
        [FromServices] IOptions<OrphanCleanupOptions> cleanupOptions,
        CancellationToken cancellationToken)
    {
        var blobServiceClient = new BlobServiceClient(blobOptions.ConnectionString);
        var container = blobServiceClient.GetBlobContainerClient(blobOptions.ContainerName);
        var blobs = new List<BlobItem>();

        if (await container.ExistsAsync(cancellationToken))
        {
            await foreach (var blob in container.GetBlobsAsync(
                               traits: BlobTraits.None,
                               states: BlobStates.None,
                               prefix: BlobUploadFolders.PublicAnnouncements,
                               cancellationToken: cancellationToken))
            {
                blobs.Add(blob);
            }
        }

        var referencedKeys = await db.PublicAnnouncementDrafts
            .AsNoTracking()
            .Where(a => a.ImageBlobKey != null && a.ImageBlobKey != "")
            .Select(a => a.ImageBlobKey!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var referencedSet = new HashSet<string>(referencedKeys, StringComparer.Ordinal);
        var graceThreshold = DateTimeOffset.UtcNow - cleanupOptions.Value.GracePeriod;
        var orphanBlobs = blobs
            .Where(blob => !referencedSet.Contains(blob.Name))
            .ToList();

        var status = new PublicAnnouncementCleanupStatusDto(
            $"blob://{blobOptions.ContainerName}/{BlobUploadFolders.PublicAnnouncements}",
            blobs.Count,
            referencedSet.Count,
            orphanBlobs.Count,
            orphanBlobs.Count(blob => blob.Properties.LastModified is { } lastModified && lastModified < graceThreshold),
            cleanupOptions.Value.GracePeriod,
            cleanupOptions.Value.DryRun,
            DateTime.UtcNow);

        return Ok(status);
    }

    [HttpPost]
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

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("image")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        [FromForm] IFormFile file,
        [FromServices] IPublicAnnouncementImageStore imageStore,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "ImageRequired", message = "Image file is required." });
        }

        if (file.Length > 8 * 1024 * 1024)
        {
            return BadRequest(new { error = "ImageTooLarge", message = "Image must be 8 MB or smaller." });
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "InvalidImageType", message = "Only image files are supported." });
        }

        var bytes = await file.ToByteArrayAsync(cancellationToken);
        if (bytes == null || bytes.Length == 0)
        {
            return BadRequest(new { error = "ImageRequired", message = "Image file is required." });
        }

        try
        {
            var result = await imageStore.SaveAsync(bytes, file.FileName, file.ContentType, cancellationToken);
            var imageUrl = Url.ActionLink(
                action: nameof(GetImage),
                controller: null,
                values: new { imageKey = result.ImageKey });

            return Ok(new { imageBlobKey = result.ImageKey, imageUrl });
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { error = "InvalidImageContent", message = "Uploaded file is not a valid image." });
        }
    }

    [HttpGet("image/{*imageKey}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImage(
        string imageKey,
        [FromServices] IPublicAnnouncementImageStore imageStore,
        CancellationToken cancellationToken)
    {
        var image = await imageStore.OpenAsync(imageKey, cancellationToken);
        if (image == null)
        {
            return NotFound(new { error = "ImageNotFound", message = "Announcement image was not found." });
        }

        return File(image.Content, image.ContentType, enableRangeProcessing: true);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("image/{*imageKey}")]
    public async Task<IActionResult> DeleteImage(
        string imageKey,
        [FromServices] IPublicAnnouncementImageStore imageStore,
        CancellationToken cancellationToken)
    {
        await imageStore.DeleteAsync(imageKey, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{draftKey:guid}")]
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

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{draftKey:guid}/preview")]
    public async Task<IActionResult> Preview(Guid draftKey)
    {
        var response = await _mediator.Send(new PreviewPublicAnnouncementDraftQuery(draftKey));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{draftKey:guid}/submit")]
    public async Task<IActionResult> SubmitForApproval(Guid draftKey)
    {
        var response = await _mediator.Send(new TransitionPublicAnnouncementDraftCommand(
            draftKey,
            PublicAnnouncementStatus.PendingApproval));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{draftKey:guid}/approve")]
    public async Task<IActionResult> Approve(Guid draftKey)
    {
        var response = await _mediator.Send(new TransitionPublicAnnouncementDraftCommand(
            draftKey,
            PublicAnnouncementStatus.Approved));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{draftKey:guid}/reject")]
    public async Task<IActionResult> Reject(Guid draftKey)
    {
        var response = await _mediator.Send(new TransitionPublicAnnouncementDraftCommand(
            draftKey,
            PublicAnnouncementStatus.Rejected));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{draftKey:guid}/publish")]
    public async Task<IActionResult> Publish(Guid draftKey)
    {
        var response = await _mediator.Send(new PublishPublicAnnouncementDraftCommand(draftKey));
        return response.ToActionResult(this);
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{draftKey:guid}")]
    public async Task<IActionResult> Delete(Guid draftKey)
    {
        var response = await _mediator.Send(new TransitionPublicAnnouncementDraftCommand(
            draftKey,
            PublicAnnouncementStatus.Deleted));
        return response.ToActionResult(this);
    }
}
