using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <summary>
/// What the dues module needs to know about a kurin's shape: whether it keeps its people in гуртки
/// or each in the kurin's own box, and which tags they carry — the tags set a senior kurin's rates.
/// </summary>
public interface IDuesKurinShape
{
    /// <summary>A senior kurin (УСП, УПС) has no гуртки: every person has an account in the kurin's box.</summary>
    Task<bool> IsSeniorAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>The kurin's tags on each of its people now, by member. Someone without tags is absent.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>>> TagsByMemberAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class DuesKurinShape : IDuesKurinShape
{
    private readonly IUnitOfWork _unitOfWork;

    public DuesKurinShape(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> IsSeniorAsync(Guid kurinKey, CancellationToken cancellationToken = default)
    {
        var kurin = await _unitOfWork.Kurins.GetByKeyAsync(kurinKey, cancellationToken);
        return kurin is not null && KurinBranchSet.IsSenior(kurin.Branch);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>>> TagsByMemberAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        => (await _unitOfWork.KurinTags.GetAssignmentsAsync(kurinKey, cancellationToken))
            .GroupBy(a => a.MemberKey)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)g.Select(a => a.KurinTagKey).ToList());
}
