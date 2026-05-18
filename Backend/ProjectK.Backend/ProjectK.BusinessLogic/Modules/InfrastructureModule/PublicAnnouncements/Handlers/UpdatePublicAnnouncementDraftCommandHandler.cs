using MediatR;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Commands;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;

public sealed class UpdatePublicAnnouncementDraftCommandHandler
    : IRequestHandler<UpdatePublicAnnouncementDraftCommand, ServiceResult<PublicAnnouncementDraftDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IPublicAnnouncementRenderer _renderer;
    private readonly IActivityLogger _activityLogger;
    private readonly IPublicAnnouncementImageStore _imageStore;

    public UpdatePublicAnnouncementDraftCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        IPublicAnnouncementRenderer renderer,
        IActivityLogger activityLogger,
        IPublicAnnouncementImageStore imageStore)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _renderer = renderer;
        _activityLogger = activityLogger;
        _imageStore = imageStore;
    }

    public async Task<ServiceResult<PublicAnnouncementDraftDto>> Handle(
        UpdatePublicAnnouncementDraftCommand request,
        CancellationToken cancellationToken)
    {
        var draft = await _unitOfWork.PublicAnnouncements.GetByKeyAsync(request.DraftKey, cancellationToken);
        if (draft == null || draft.Status == PublicAnnouncementStatus.Deleted)
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.NotFound, "DraftNotFound", "Announcement draft not found.");
        }

        if (draft.Status == PublicAnnouncementStatus.Published)
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.BadRequest, "DraftAlreadyPublished", "Published announcements cannot be edited.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.BadRequest, "InvalidAnnouncementDraft", "Title and body are required.");
        }

        if (PublicAnnouncementContentGuard.ContainsSensitiveData(request.Title, request.Body, request.ImageAltText, out var reason))
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(
                ResultType.BadRequest,
                "DraftContainsSensitiveData",
                $"Announcement draft contains sensitive data ({reason}).");
        }

        var previousImageBlobKey = draft.ImageBlobKey;
        var nextImageBlobKey = NormalizeOptional(request.ImageBlobKey);

        draft.Title = request.Title.Trim();
        draft.Body = request.Body.Trim();
        draft.ParseMode = request.ParseMode;
        draft.ImageBlobKey = nextImageBlobKey;
        draft.ImageUrl = NormalizeOptional(request.ImageUrl);
        draft.ImageAltText = NormalizeOptional(request.ImageAltText);
        draft.ImagePlacement = request.ImagePlacement;
        draft.TemplateKey = NormalizeOptional(request.TemplateKey);
        draft.TemplateDataJson = NormalizeOptional(request.TemplateDataJson);
        draft.RenderedText = _renderer.Render(draft).RenderedText;
        draft.UpdatedAtUtc = DateTime.UtcNow;
        draft.UpdatedByUserKey = _currentUserContext.UserId;

        if (draft.Status == PublicAnnouncementStatus.Approved)
        {
            draft.Status = PublicAnnouncementStatus.Draft;
            draft.ApprovedAtUtc = null;
            draft.ApprovedByUserKey = null;
        }

        _unitOfWork.PublicAnnouncements.Update(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousImageBlobKey)
            && !string.Equals(previousImageBlobKey, nextImageBlobKey, StringComparison.Ordinal))
        {
            await _imageStore.DeleteAsync(previousImageBlobKey, cancellationToken);
        }

        _activityLogger.LogAudit(
            action: "Announcement.Updated",
            actorUserId: _currentUserContext.UserId,
            reason: $"DraftKey={draft.PublicAnnouncementDraftKey}; Status={draft.Status}; Title={draft.Title}");

        return new ServiceResult<PublicAnnouncementDraftDto>(ResultType.Success, PublicAnnouncementMapper.ToDto(draft));
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
