namespace ProjectK.Common.Models.Dtos.InfrastructureModule;

public sealed record PublicAnnouncementCleanupStatusDto(
    string ImageStorePath,
    int TotalLocalImages,
    int ReferencedLocalImages,
    int OrphanLocalImages,
    int EligibleForDeletion,
    TimeSpan GracePeriod,
    bool DryRun,
    DateTime CheckedAtUtc);
