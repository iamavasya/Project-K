using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;

/// <inheritdoc />
public sealed class MemberProgressDirectory : IMemberProgressDirectory
{
    private readonly IUnitOfWork _unitOfWork;

    public MemberProgressDirectory(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KurinProgressFacts> GetFactsForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default)
    {
        var badges = await _unitOfWork.BadgeProgresses.GetByKurinKeyAsync(kurinKey, cancellationToken);
        var points = await _unitOfWork.ProbePointProgresses.GetSignedByKurinKeyAsync(kurinKey, cancellationToken);
        var probes = await _unitOfWork.ProbeProgresses.GetByKurinKeyAsync(kurinKey, cancellationToken);

        return new KurinProgressFacts(
            [.. badges
                .Where(b => b.Status == BadgeProgressStatus.Confirmed && b.ReviewedAtUtc.HasValue)
                .Select(b => new BadgeConfirmedRecord(b.MemberKey, b.BadgeId, b.ReviewedAtUtc!.Value))],
            [.. points
                .Where(p => p.SignedAtUtc.HasValue)
                .Select(p => new ProbePointSignedRecord(p.MemberKey, p.ProbeId, p.PointId, p.SignedAtUtc!.Value))],
            [.. probes
                .Where(p => p.Status == ProbeProgressStatus.Verified && p.VerifiedAtUtc.HasValue)
                .Select(p => new ProbeClosedRecord(p.MemberKey, p.ProbeId, p.VerifiedAtUtc!.Value))]);
    }

    public async Task<MemberProgress> GetForMemberAsync(
        Guid memberKey,
        CancellationToken cancellationToken = default)
    {
        var probes = await _unitOfWork.ProbeProgresses.GetByMemberKeyAsync(memberKey, cancellationToken);
        var badges = await _unitOfWork.BadgeProgresses.GetByMemberKeyAsync(memberKey, cancellationToken);

        return new MemberProgress(
            [.. probes
                .OrderBy(progress => progress.ProbeId)
                .Select(progress => new ProbeProgressRecord(
                    progress.ProbeId,
                    progress.Status,
                    progress.KurinKey,
                    progress.CompletedAtUtc,
                    progress.VerifiedAtUtc))],
            [.. badges
                .OrderBy(progress => progress.BadgeId)
                .Select(progress => new BadgeProgressRecord(
                    progress.BadgeId,
                    progress.Status,
                    progress.KurinKey,
                    progress.SubmittedAtUtc,
                    progress.ReviewedAtUtc))]);
    }

    public async Task<IReadOnlyCollection<string>> GetSignedPointIdsAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default)
        => [.. (await _unitOfWork.ProbePointProgresses.GetByMemberAndProbeAsync(memberKey, probeId, cancellationToken))
            .Where(p => p.IsSigned)
            .Select(p => p.PointId)];

    public async Task<int> CountForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
        => await _unitOfWork.ProbeProgresses.CountByMemberKeyAsync(memberKey, cancellationToken)
           + await _unitOfWork.BadgeProgresses.CountByMemberKeyAsync(memberKey, cancellationToken);
}
