using MediatR;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Commands;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;

public sealed class CreatePublicAnnouncementDraftCommandHandler
    : IRequestHandler<CreatePublicAnnouncementDraftCommand, ServiceResult<PublicAnnouncementDraftDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IPublicAnnouncementRenderer _renderer;
    private readonly IActivityLogger _activityLogger;

    public CreatePublicAnnouncementDraftCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        IPublicAnnouncementRenderer renderer,
        IActivityLogger activityLogger)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _renderer = renderer;
        _activityLogger = activityLogger;
    }

    public async Task<ServiceResult<PublicAnnouncementDraftDto>> Handle(
        CreatePublicAnnouncementDraftCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(
                ResultType.BadRequest,
                "InvalidAnnouncementDraft",
                "Title and body are required.");
        }

        if (PublicAnnouncementContentGuard.ContainsSensitiveData(request.Title, request.Body, request.ImageAltText, out var reason))
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(
                ResultType.BadRequest,
                "DraftContainsSensitiveData",
                $"Announcement draft contains sensitive data ({reason}).");
        }

        var sourceId = NormalizeOptional(request.SourceId);
        if (!string.IsNullOrWhiteSpace(sourceId))
        {
            var duplicate = await _unitOfWork.PublicAnnouncements.GetActiveBySourceAsync(
                request.SourceType,
                sourceId,
                cancellationToken: cancellationToken);

            if (duplicate != null)
            {
                return ServiceResult<PublicAnnouncementDraftDto>.Failure(
                    ResultType.Conflict,
                    "DuplicateAnnouncementSource",
                    "An active announcement draft already exists for this source.");
            }
        }

        var draft = new PublicAnnouncementDraft
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            SourceType = request.SourceType,
            SourceId = sourceId,
            SourceUrl = NormalizeOptional(request.SourceUrl),
            Environment = NormalizeOptional(request.Environment),
            Version = NormalizeOptional(request.Version),
            Codename = NormalizeOptional(request.Codename),
            ParseMode = request.ParseMode,
            ImageBlobKey = NormalizeOptional(request.ImageBlobKey),
            ImageUrl = NormalizeOptional(request.ImageUrl),
            ImageAltText = NormalizeOptional(request.ImageAltText),
            ImagePlacement = request.ImagePlacement,
            TemplateKey = NormalizeOptional(request.TemplateKey),
            TemplateDataJson = NormalizeOptional(request.TemplateDataJson),
            CreatedByUserKey = _currentUserContext.UserId,
            UpdatedByUserKey = _currentUserContext.UserId
        };

        draft.RenderedText = _renderer.Render(draft).RenderedText;

        _unitOfWork.PublicAnnouncements.Create(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _activityLogger.LogAudit(
            action: "Announcement.Created",
            actorUserId: _currentUserContext.UserId,
            reason: BuildAuditReason(draft));

        return new ServiceResult<PublicAnnouncementDraftDto>(
            ResultType.Created,
            PublicAnnouncementMapper.ToDto(draft),
            CreatedAtActionName: "GetByKey",
            CreatedAtRouteValues: new { draftKey = draft.PublicAnnouncementDraftKey });
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string BuildAuditReason(PublicAnnouncementDraft draft)
    {
        return $"DraftKey={draft.PublicAnnouncementDraftKey}; Status={draft.Status}; Source={draft.SourceType}:{draft.SourceId}; Title={draft.Title}";
    }
}
