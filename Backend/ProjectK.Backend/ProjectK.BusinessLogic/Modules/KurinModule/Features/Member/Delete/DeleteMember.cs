using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Delete
{
    public class DeleteMember : IRequest<ServiceResult<object>>
    {
        public Guid MemberKey { get; set; }
        public DeleteMember(Guid memberKey)
        {
            MemberKey = memberKey;
        }
    }

    public class DeleteMemberHandler : IRequestHandler<DeleteMember, ServiceResult<object>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeleteMemberHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<object>> Handle(DeleteMember request, CancellationToken cancellationToken)
        {
            if (request.MemberKey == Guid.Empty)
            {
                return new ServiceResult<object>(
                    ResultType.BadRequest,
                    "MemberKey cannot be empty.");
            }
            var existing = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
            if (existing is null)
            {
                return new ServiceResult<object>(
                    ResultType.NotFound,
                    $"Member with key {request.MemberKey} not found.");
            }
            _unitOfWork.Members.Delete(existing, cancellationToken);
            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<object>(
                    ResultType.InternalServerError,
                    "Failed to delete Member due to internal error.");
            }
            return new ServiceResult<object>(ResultType.Success);
        }
    }
}
