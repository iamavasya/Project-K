using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward.Delete
{
    public sealed class DeleteMemberAward : IRequest<ServiceResult<Unit>>
    {
        public Guid MemberAwardKey { get; set; }
    }

    public sealed class DeleteMemberAwardHandler : IRequestHandler<DeleteMemberAward, ServiceResult<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserContext _currentUser;

        public DeleteMemberAwardHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<ServiceResult<Unit>> Handle(DeleteMemberAward request, CancellationToken cancellationToken)
        {
            var award = await _unitOfWork.MemberAwards.GetByKeyAsync(request.MemberAwardKey, cancellationToken);
            if (award is null)
            {
                return new ServiceResult<Unit>(ResultType.NotFound);
            }

            // Confirming an award takes leadership, so undoing a confirmed one must too. Withdrawing
            // your own submission stays open to the member who made it.
            if (award.Status == BadgeProgressStatus.Confirmed && !_currentUser.CanLeadGroups())
            {
                return ServiceResult<Unit>.Failure(
                    ResultType.Forbidden,
                    "ConfirmedAwardRequiresLeadership",
                    "Only leadership may remove a confirmed award.");
            }

            _unitOfWork.MemberAwards.Delete(award);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Unit>(ResultType.Success, Unit.Value);
        }
    }
}
