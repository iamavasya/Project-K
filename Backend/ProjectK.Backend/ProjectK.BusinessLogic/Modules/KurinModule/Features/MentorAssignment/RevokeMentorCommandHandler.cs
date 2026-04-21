using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment
{
    public class RevokeMentorCommandHandler : IRequestHandler<RevokeMentorCommand, ServiceResult<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RevokeMentorCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<bool>> Handle(RevokeMentorCommand request, CancellationToken cancellationToken)
        {
            var existingAssignment = await _unitOfWork.MentorAssignments.GetSpecificAssignmentAsync(request.MentorUserKey, request.GroupKey, cancellationToken);

            if (existingAssignment == null)
            {
                return new ServiceResult<bool>(ResultType.NotFound, false, "Mentor assignment not found.");
            }

            if (existingAssignment.RevokedAtUtc != null)
            {
                return new ServiceResult<bool>(ResultType.Success, true, "Assignment was already revoked.");
            }

            existingAssignment.RevokedAtUtc = DateTime.UtcNow;
            _unitOfWork.MentorAssignments.Update(existingAssignment, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
