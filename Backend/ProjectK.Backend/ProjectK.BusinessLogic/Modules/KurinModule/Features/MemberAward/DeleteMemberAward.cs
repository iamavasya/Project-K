using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward
{
    public sealed class DeleteMemberAward : IRequest<ServiceResult<Unit>>
    {
        public Guid MemberAwardKey { get; set; }
    }

    public sealed class DeleteMemberAwardHandler : IRequestHandler<DeleteMemberAward, ServiceResult<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteMemberAwardHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<Unit>> Handle(DeleteMemberAward request, CancellationToken cancellationToken)
        {
            var award = await _unitOfWork.MemberAwards.GetByKeyAsync(request.MemberAwardKey, cancellationToken);
            if (award is null)
            {
                return new ServiceResult<Unit>(ResultType.NotFound);
            }

            _unitOfWork.MemberAwards.Delete(award);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Unit>(ResultType.Success, Unit.Value);
        }
    }
}
