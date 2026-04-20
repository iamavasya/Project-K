using MediatR;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment
{
    public class AssignMentorCommandHandler : IRequestHandler<AssignMentorCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AssignMentorCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<Guid>> Handle(AssignMentorCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey, cancellationToken);
            if (group == null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound, Guid.Empty, "Group not found.");
            }

            var mentorMember = await _unitOfWork.Members.GetByUserKeyAsync(request.MentorUserKey, cancellationToken);
            if (mentorMember == null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound, Guid.Empty, "Mentor member profile not found.");
            }

            if (mentorMember.KurinKey != group.KurinKey)
            {
                return new ServiceResult<Guid>(ResultType.Forbidden, Guid.Empty, "Mentor must belong to the same Kurin as the Group.");
            }

            var existingAssignment = await _unitOfWork.MentorAssignments.GetSpecificAssignmentAsync(request.MentorUserKey, request.GroupKey, cancellationToken);
            if (existingAssignment != null && existingAssignment.RevokedAtUtc == null)
            {
                return new ServiceResult<Guid>(ResultType.Conflict, existingAssignment.MentorAssignmentKey, "Mentor is already assigned to this group.");
            }

            var assignment = new ProjectK.Common.Entities.KurinModule.MentorAssignment
            {
                MentorAssignmentKey = Guid.NewGuid(),
                MentorUserKey = request.MentorUserKey,
                GroupKey = request.GroupKey,
                AssignedAtUtc = DateTime.UtcNow
            };

            _unitOfWork.MentorAssignments.Create(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, assignment.MentorAssignmentKey);
        }
    }
}
