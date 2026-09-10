using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Membership.MoveToGroup
{
    /// <summary>
    /// Moves someone between гуртки inside one kurin, or takes them out of one altogether — belonging
    /// to a kurin and to no гурток is a state a membership is allowed to be in.
    /// </summary>
    public sealed record MoveToGroup(Guid MemberKey, Guid KurinKey, Guid? GroupKey)
        : IRequest<ServiceResult<object>>;

    public sealed class MoveToGroupHandler : IRequestHandler<MoveToGroup, ServiceResult<object>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public MoveToGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<object>> Handle(MoveToGroup request, CancellationToken cancellationToken)
        {
            if (request.MemberKey == Guid.Empty || request.KurinKey == Guid.Empty)
            {
                return ServiceResult<object>.Failure(
                    ResultType.BadRequest,
                    "MembershipKeysRequired",
                    "Both a member and a kurin are required.");
            }

            var membership = await _unitOfWork.Memberships.GetActiveAsync(
                request.MemberKey, request.KurinKey, cancellationToken);

            if (membership is null)
            {
                return ServiceResult<object>.Failure(
                    ResultType.NotFound, "NotAMember", "They do not currently belong to this kurin.");
            }

            if (request.GroupKey.HasValue)
            {
                var group = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey.Value, cancellationToken);
                if (group is null || group.KurinKey != request.KurinKey)
                {
                    return ServiceResult<object>.Failure(
                        ResultType.BadRequest, "GroupNotInKurin", "That гурток does not belong to this kurin.");
                }
            }

            membership.GroupKey = request.GroupKey;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<object>(ResultType.Success);
        }
    }
}
