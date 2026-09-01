using MediatR;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Delete
{
    public sealed record DeleteGroup(Guid GroupKey) : IRequest<ServiceResult<object>>;

    public sealed class DeleteGroupHandler : IRequestHandler<DeleteGroup, ServiceResult<object>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackendCache _cache;
        public DeleteGroupHandler(IUnitOfWork unitOfWork, IBackendCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }
        public async Task<ServiceResult<object>> Handle(DeleteGroup request, CancellationToken cancellationToken)
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

            // Everything the гурток holds goes first: its провід is Restrict, its members are
            // NoAction, so the database refuses to delete a гурток that still has either. Mentor
            // assignments and the members' own history cascade on their own.
            await _unitOfWork.Leaderships.DeleteForGroupAsync(request.GroupKey, cancellationToken);

            var members = await _unitOfWork.Members.GetAllAsync(request.GroupKey, cancellationToken);

            foreach (var member in members)
            {
                _unitOfWork.Members.Delete(member, cancellationToken);
            }

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
}
