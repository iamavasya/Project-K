using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
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

    public async Task<int> CountForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
        => await _unitOfWork.ProbeProgresses.CountByMemberKeyAsync(memberKey, cancellationToken)
           + await _unitOfWork.BadgeProgresses.CountByMemberKeyAsync(memberKey, cancellationToken);
}
