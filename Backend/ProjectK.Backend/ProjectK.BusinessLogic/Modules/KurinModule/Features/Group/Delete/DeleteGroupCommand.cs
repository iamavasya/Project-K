using MediatR;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Delete;

public sealed record DeleteGroupCommand(Guid GroupKey) : IRequest<ServiceResult<object>>;

public sealed class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand, ServiceResult<object>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackendCache _cache;
    public DeleteGroupCommandHandler(IUnitOfWork unitOfWork,
        IBackendCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }
    public async Task<ServiceResult<object>> Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        if (request.GroupKey == Guid.Empty)
        {
            return ServiceResult<object>.Failure(
                ResultType.BadRequest,
                "GROUP_KEY_EMPTY",
                "GroupKey cannot be empty.");
        }
        var existing = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey, cancellationToken);
        if (existing is null)
        {
            return ServiceResult<object>.Failure(
                ResultType.NotFound,
                "GROUP_NOT_FOUND",
                $"Group with key {request.GroupKey} not found.");
        }

        // Its провід goes first — that one is Restrict, so the database refuses to delete a
        // гурток that still carries one. Mentor assignments cascade on their own.
        var leadershipKeys = await _unitOfWork.Leaderships.DeleteForGroupAsync(request.GroupKey, cancellationToken);

        // The people stay, and stay in the kurin — they are simply no longer in a гурток, which
        // is a state a membership is allowed to be in. Dissolving a гурток is not a reason for
        // anyone to leave.
        await _unitOfWork.Memberships.DetachFromGroupAsync(request.GroupKey, cancellationToken);

        // Agenda assignments name their target by a bare key, so nothing in the database clears
        // them: both the гурток and its offices are valid targets and are about to disappear.
        await _unitOfWork.AgendaItems.RemoveAssignmentsForTargetsAsync(
            [request.GroupKey, .. leadershipKeys],
            cancellationToken);

        _unitOfWork.Groups.Delete(existing, cancellationToken);
        var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (changes <= 0)
        {
            return ServiceResult<object>.Failure(
                ResultType.InternalServerError,
                "GROUP_DELETE_FAILED",
                "Failed to delete Group due to internal error.");
        }
        _cache.Invalidate(BackendCachePolicies.GroupReads);

        return new ServiceResult<object>(ResultType.Success);
    }
}
