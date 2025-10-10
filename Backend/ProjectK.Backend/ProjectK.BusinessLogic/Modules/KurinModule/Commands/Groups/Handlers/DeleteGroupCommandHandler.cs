using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Groups.Handlers
{
    public class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand, ServiceResult<object>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeleteGroupCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ServiceResult<object>> Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
        {
            if (request.GroupKey == Guid.Empty)
            {
                return new ServiceResult<object>(
                    ResultType.BadRequest,
                    "GroupKey cannot be empty.");
            }
            var existing = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey, cancellationToken);
            if (existing is null)
            {
                return new ServiceResult<object>(
                    ResultType.NotFound,
                    $"Group with key {request.GroupKey} not found.");
            }

            // Delete all members with GroupKey
            var members = await _unitOfWork.Members.GetAllAsync(request.GroupKey, cancellationToken);

            foreach (var member in members)
            {
                member.Group = null;
                member.Kurin = null!;
                _unitOfWork.Members.Delete(member, cancellationToken);
            }

            _unitOfWork.Groups.Delete(existing, cancellationToken);
            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<object>(
                    ResultType.InternalServerError,
                    "Failed to delete Group due to internal error.");
            }
            return new ServiceResult<object>(ResultType.Success);
        }
    }
}
