using MediatR;
using Microsoft.Extensions.Options;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Queries;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Settings;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;

/// <summary>
/// Compares what the image store holds against what the drafts still reference.
/// <para>
/// The controller used to do this inline — building a blob client and querying the DbContext from an
/// action, while every neighbouring action just sent a query.
/// </para>
/// </summary>
public sealed class GetPublicAnnouncementCleanupStatusQueryHandler
    : IRequestHandler<GetPublicAnnouncementCleanupStatusQuery, ServiceResult<PublicAnnouncementCleanupStatusDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublicAnnouncementImageStore _imageStore;
    private readonly OrphanCleanupOptions _cleanupOptions;
    private readonly TimeProvider _timeProvider;

    public GetPublicAnnouncementCleanupStatusQueryHandler(
        IUnitOfWork unitOfWork,
        IPublicAnnouncementImageStore imageStore,
        IOptions<OrphanCleanupOptions> cleanupOptions,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _imageStore = imageStore;
        _cleanupOptions = cleanupOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceResult<PublicAnnouncementCleanupStatusDto>> Handle(
        GetPublicAnnouncementCleanupStatusQuery request,
        CancellationToken cancellationToken)
    {
        var stored = await _imageStore.ListAsync(cancellationToken);
        var referenced = await _unitOfWork.PublicAnnouncements.GetReferencedImageKeysAsync(cancellationToken);

        var referencedKeys = new HashSet<string>(referenced, StringComparer.Ordinal);
        var orphans = stored.Where(image => !referencedKeys.Contains(image.Key)).ToArray();

        var now = _timeProvider.GetUtcNow();
        var graceThreshold = now - _cleanupOptions.GracePeriod;

        var status = new PublicAnnouncementCleanupStatusDto(
            $"store://{BlobUploadFolders.PublicAnnouncements}",
            stored.Count,
            referencedKeys.Count,
            orphans.Length,
            orphans.Count(image => image.LastModified is { } lastModified && lastModified < graceThreshold),
            _cleanupOptions.GracePeriod,
            _cleanupOptions.DryRun,
            now.UtcDateTime);

        return new ServiceResult<PublicAnnouncementCleanupStatusDto>(ResultType.Success, status);
    }
}
