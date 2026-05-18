using MediatR;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Commands;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;

public sealed class TransitionPublicAnnouncementDraftCommandHandler
    : IRequestHandler<TransitionPublicAnnouncementDraftCommand, ServiceResult<PublicAnnouncementDraftDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IActivityLogger _activityLogger;
    private readonly IPublicAnnouncementImageStore _imageStore;

    public TransitionPublicAnnouncementDraftCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        IActivityLogger activityLogger,
        IPublicAnnouncementImageStore imageStore)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _activityLogger = activityLogger;
        _imageStore = imageStore;
    }

    public async Task<ServiceResult<PublicAnnouncementDraftDto>> Handle(
        TransitionPublicAnnouncementDraftCommand request,
        CancellationToken cancellationToken)
    {
        var draft = await _unitOfWork.PublicAnnouncements.GetByKeyAsync(request.DraftKey, cancellationToken);
        if (draft == null || draft.Status == PublicAnnouncementStatus.Deleted)
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.NotFound, "DraftNotFound", "Announcement draft not found.");
        }

        if (draft.Status == PublicAnnouncementStatus.Published)
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.BadRequest, "DraftAlreadyPublished", "Published announcements cannot be changed.");
        }

        var shouldDeleteImage = request.TargetStatus is PublicAnnouncementStatus.Deleted or PublicAnnouncementStatus.Rejected
            && !string.IsNullOrWhiteSpace(draft.ImageBlobKey);
        var now = DateTime.UtcNow;
        switch (request.TargetStatus)
        {
            case PublicAnnouncementStatus.PendingApproval:
                draft.Status = PublicAnnouncementStatus.PendingApproval;
                break;
            case PublicAnnouncementStatus.Approved:
                draft.Status = PublicAnnouncementStatus.Approved;
                draft.ApprovedAtUtc = now;
                draft.ApprovedByUserKey = _currentUserContext.UserId;
                break;
            case PublicAnnouncementStatus.Rejected:
                draft.Status = PublicAnnouncementStatus.Rejected;
                break;
            case PublicAnnouncementStatus.Deleted:
                draft.Status = PublicAnnouncementStatus.Deleted;
                break;
            case PublicAnnouncementStatus.Draft:
                draft.Status = PublicAnnouncementStatus.Draft;
                draft.ApprovedAtUtc = null;
                draft.ApprovedByUserKey = null;
                break;
            default:
                return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.BadRequest, "InvalidDraftTransition", "Unsupported announcement draft transition.");
        }

        draft.UpdatedAtUtc = now;
        draft.UpdatedByUserKey = _currentUserContext.UserId;

        if (shouldDeleteImage)
        {
            await _imageStore.DeleteAsync(draft.ImageBlobKey!, cancellationToken);
            draft.ImageBlobKey = null;
            draft.ImageUrl = null;
        }

        _unitOfWork.PublicAnnouncements.Update(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _activityLogger.LogAudit(
            action: GetAuditAction(request.TargetStatus),
            actorUserId: _currentUserContext.UserId,
            reason: $"DraftKey={draft.PublicAnnouncementDraftKey}; Status={draft.Status}; Title={draft.Title}");

        return new ServiceResult<PublicAnnouncementDraftDto>(ResultType.Success, PublicAnnouncementMapper.ToDto(draft));
    }

    private static string GetAuditAction(PublicAnnouncementStatus status)
    {
        return status switch
        {
            PublicAnnouncementStatus.PendingApproval => "Announcement.Submitted",
            PublicAnnouncementStatus.Approved => "Announcement.Approved",
            PublicAnnouncementStatus.Rejected => "Announcement.Rejected",
            PublicAnnouncementStatus.Deleted => "Announcement.Deleted",
            PublicAnnouncementStatus.Draft => "Announcement.Reopened",
            _ => "Announcement.StatusChanged"
        };
    }
}
