using MediatR;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Commands;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;

public sealed class PublishPublicAnnouncementDraftCommandHandler
    : IRequestHandler<PublishPublicAnnouncementDraftCommand, ServiceResult<PublicAnnouncementDraftDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IPublicAnnouncementRenderer _renderer;
    private readonly IPublicAnnouncementPublisher _publisher;
    private readonly IActivityLogger _activityLogger;
    private readonly IPublicAnnouncementImageStore _imageStore;

    public PublishPublicAnnouncementDraftCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        IPublicAnnouncementRenderer renderer,
        IPublicAnnouncementPublisher publisher,
        IActivityLogger activityLogger,
        IPublicAnnouncementImageStore imageStore)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _renderer = renderer;
        _publisher = publisher;
        _activityLogger = activityLogger;
        _imageStore = imageStore;
    }

    public async Task<ServiceResult<PublicAnnouncementDraftDto>> Handle(
        PublishPublicAnnouncementDraftCommand request,
        CancellationToken cancellationToken)
    {
        var draft = await _unitOfWork.PublicAnnouncements.GetByKeyAsync(request.DraftKey, cancellationToken);
        if (draft == null || draft.Status == PublicAnnouncementStatus.Deleted)
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.NotFound, "DraftNotFound", "Announcement draft not found.");
        }

        if (draft.Status == PublicAnnouncementStatus.Published)
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(
                ResultType.Conflict,
                "DraftAlreadyPublished",
                "Announcement draft has already been published.");
        }

        if (draft.Status != PublicAnnouncementStatus.Approved)
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.BadRequest, "DraftNotApproved", "Announcement draft must be approved before publishing.");
        }

        if (!string.IsNullOrWhiteSpace(draft.TelegramMessageId))
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(
                ResultType.Conflict,
                "DraftAlreadySent",
                "Announcement draft already has Telegram message metadata and cannot be published again.");
        }

        if (PublicAnnouncementContentGuard.ContainsSensitiveData(draft.Title, draft.Body, draft.ImageAltText, out var reason))
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(
                ResultType.BadRequest,
                "DraftContainsSensitiveData",
                $"Announcement draft contains sensitive data ({reason}).");
        }

        var preview = _renderer.Render(draft);
        if (preview.Warnings.Any(w => w.Contains("exceeds Telegram message limit", StringComparison.OrdinalIgnoreCase)))
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.BadRequest, "DraftInvalidForTelegram", "Announcement draft exceeds Telegram message limits.");
        }

        var result = await _publisher.PublishAsync(draft, preview.RenderedText, cancellationToken);
        var now = DateTime.UtcNow;

        draft.RenderedText = preview.RenderedText;
        draft.UpdatedAtUtc = now;
        draft.UpdatedByUserKey = _currentUserContext.UserId;

        if (result.Succeeded)
        {
            draft.Status = PublicAnnouncementStatus.Published;
            draft.PublishedAtUtc = now;
            draft.PublishedByUserKey = _currentUserContext.UserId;
            draft.TelegramMessageId = result.TelegramMessageId;
            draft.LastPublishError = null;
        }
        else
        {
            draft.Status = PublicAnnouncementStatus.Failed;
            if (result.PartiallySucceeded)
            {
                draft.TelegramMessageId = result.TelegramMessageId;
            }
            draft.LastPublishError = result.ErrorMessage;
        }

        if ((result.Succeeded || result.PartiallySucceeded) && !string.IsNullOrWhiteSpace(draft.ImageBlobKey))
        {
            await _imageStore.DeleteAsync(draft.ImageBlobKey, cancellationToken);
            draft.ImageBlobKey = null;
            draft.ImageUrl = null;
        }

        _unitOfWork.PublicAnnouncements.Update(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _activityLogger.LogAudit(
            action: result.Succeeded ? "Announcement.Published" : "Announcement.PublishFailed",
            actorUserId: _currentUserContext.UserId,
            reason: $"DraftKey={draft.PublicAnnouncementDraftKey}; Status={draft.Status}; TelegramMessageId={draft.TelegramMessageId}; Partial={result.PartiallySucceeded}; Title={draft.Title}");

        return result.Succeeded
            ? new ServiceResult<PublicAnnouncementDraftDto>(ResultType.Success, PublicAnnouncementMapper.ToDto(draft))
            : ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.BadRequest, "TelegramPublishFailed", result.ErrorMessage ?? "Failed to publish Telegram announcement.");
    }
}
