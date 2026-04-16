namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;

public sealed record ProbePointProgressResponse(
    Guid? ProbePointProgressKey,
    string PointId,
    bool IsSigned,
    DateTime? SignedAtUtc,
    Guid? SignedByUserKey,
    string? SignedByName,
    string? SignedByRole);
