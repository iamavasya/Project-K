using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <inheritdoc />
public sealed class MemberDirectory : IMemberDirectory
{
    private readonly IUnitOfWork _unitOfWork;

    public MemberDirectory(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<bool> ExistsAsync(Guid memberKey, CancellationToken cancellationToken = default)
        => _unitOfWork.Members.ExistsAsync(memberKey, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _unitOfWork.Members.ExistsByEmailAsync(email, cancellationToken);

    public Task<MemberSummary?> FindAsync(Guid memberKey, CancellationToken cancellationToken = default)
        => _unitOfWork.Members.GetSummaryByKeyAsync(memberKey, cancellationToken);

    public Task<MemberSummary?> FindByAccountAsync(Guid userKey, CancellationToken cancellationToken = default)
        => _unitOfWork.Members.GetSummaryByUserKeyAsync(userKey, cancellationToken);

    public Task<Guid?> FindAccountKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
        => _unitOfWork.Members.GetUserKeyByMemberAsync(memberKey, cancellationToken);

    public Task<Guid?> FindKurinKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
        => _unitOfWork.Members.GetKurinKeyByMemberAsync(memberKey, cancellationToken);

    public Task<IReadOnlyCollection<MemberSummary>> GetByKurinAsync(
        Guid kurinKey,
        CancellationToken cancellationToken = default)
        => _unitOfWork.Members.GetSummariesByKurinKeyAsync(kurinKey, cancellationToken);

    public async Task<IReadOnlyCollection<MemberLookupDto>> GetLookupByKurinAsync(
        Guid kurinKey,
        CancellationToken cancellationToken = default)
        => (await _unitOfWork.Members.GetMentorCandidatesLookupAsync(kurinKey, cancellationToken)).ToList();

    public Task<IReadOnlyCollection<MemberSummary>> GetAllAsync(CancellationToken cancellationToken = default)
        => _unitOfWork.Members.GetAllSummariesAsync(cancellationToken);

    public async Task<Guid> EnsureForAccountAsync(
        MemberForAccount details,
        CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Members.GetByEmailAsync(details.Email, cancellationToken);
        if (existing is not null)
        {
            existing.UserKey = details.UserKey;
            _unitOfWork.Members.Update(existing, cancellationToken);
            return existing.MemberKey;
        }

        var member = new Member
        {
            MemberKey = Guid.NewGuid(),
            FirstName = details.FirstName,
            LastName = details.LastName,
            Email = details.Email,
            PhoneNumber = details.PhoneNumber,
            DateOfBirth = details.DateOfBirth,
            UserKey = details.UserKey,
            KurinKey = details.KurinKey
        };

        _unitOfWork.Members.Create(member, cancellationToken);
        return member.MemberKey;
    }

    public async Task SetEmailFromAccountAsync(
        Guid userKey,
        string email,
        CancellationToken cancellationToken = default)
    {
        var member = await _unitOfWork.Members.GetTrackedByUserKeyAsync(userKey, cancellationToken);
        if (member is null)
        {
            return;
        }

        member.Email = email;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPhoneFromAccountAsync(
        Guid userKey,
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var member = await _unitOfWork.Members.GetTrackedByUserKeyAsync(userKey, cancellationToken);
        if (member is null)
        {
            return;
        }

        member.PhoneNumber = phoneNumber;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
