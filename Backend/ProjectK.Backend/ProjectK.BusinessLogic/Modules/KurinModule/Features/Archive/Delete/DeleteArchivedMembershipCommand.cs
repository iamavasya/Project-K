using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Audit;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Archive.Delete;

/// <summary>
/// Deletes a membership that has ended, in the danger zone, as if the person had never been in the
/// kurin: the membership, the гуртки it passed through, and the offices they held here in that time.
/// The person stays — their probes, badges and awards are theirs — and so do the kurin's books.
/// </summary>
public sealed record DeleteArchivedMembershipCommand(Guid KurinKey, Guid MembershipKey, string? Ticket) : IRequest<ServiceResult<ArchiveDeletionDto>>;

public sealed class DeleteArchivedMembershipCommandHandler : IRequestHandler<DeleteArchivedMembershipCommand, ServiceResult<ArchiveDeletionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberDirectory _members;
    private readonly ArchiveDeletionGate _gate;
    private readonly IKurinAudit _audit;

    public DeleteArchivedMembershipCommandHandler(IUnitOfWork unitOfWork, IMemberDirectory members, ArchiveDeletionGate gate, IKurinAudit audit)
    {
        _unitOfWork = unitOfWork;
        _members = members;
        _gate = gate;
        _audit = audit;
    }

    public async Task<ServiceResult<ArchiveDeletionDto>> Handle(DeleteArchivedMembershipCommand request, CancellationToken cancellationToken)
    {
        var (pass, refusal) = await _gate.OpenAsync(request.KurinKey, request.Ticket, cancellationToken);
        if (pass is null)
        {
            return refusal!;
        }

        var membership = await _unitOfWork.Archive.FindMembershipAsync(request.MembershipKey, cancellationToken);
        if (membership is null || membership.KurinKey != request.KurinKey)
        {
            return ServiceResult<ArchiveDeletionDto>.Failure(ResultType.NotFound, ArchiveErrors.NotFound, "No such membership in this kurin.");
        }

        if (membership.LeftAtUtc is not { } leftAt)
        {
            return ServiceResult<ArchiveDeletionDto>.Failure(ResultType.Conflict, ArchiveErrors.StillCurrent, "A current membership is ended first.");
        }

        if (pass.Reach.IsOwn(membership.MemberKey))
        {
            return ServiceResult<ArchiveDeletionDto>.Failure(ResultType.Forbidden, ArchiveErrors.OwnRecord, "Nobody deletes their own record.");
        }

        if (await _gate.SpendAsync(pass, request.KurinKey, cancellationToken) is not { } left)
        {
            return ArchiveDeletionGate.Closed();
        }

        var person = (await _members.GetByKeysAsync([membership.MemberKey], cancellationToken)).FirstOrDefault()?.FullName ?? string.Empty;
        var joinedOn = DateOnly.FromDateTime(membership.JoinedAtUtc);
        var leftOn = DateOnly.FromDateTime(leftAt);
        var offices = await _unitOfWork.Archive.RemoveOfficesHeldAsync(membership.MemberKey, request.KurinKey, joinedOn, leftOn, cancellationToken);
        _unitOfWork.Archive.RemoveMembership(membership);

        await _audit.RecordAsync(
            new AuditRecord(
                AuditArea.Archive,
                AuditActions.Archive.MembershipDeleted,
                membership.MembershipKey,
                person,
                membership.MemberKey,
                AuditChanges.Removed(
                    ("holder", person),
                    ("kind", membership.Kind),
                    ("joinedOn", joinedOn),
                    ("leftOn", leftOn),
                    ("offices", offices.Count == 0 ? null : offices.Count)),
                KurinKey: request.KurinKey),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ServiceResult<ArchiveDeletionDto>(ResultType.Success, new ArchiveDeletionDto(left));
    }
}
