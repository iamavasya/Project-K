using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment
{
    public record GetKurinMentorAssignmentsQuery(Guid KurinKey) : IRequest<ServiceResult<IEnumerable<MentorAssignmentDto>>>;

    public class GetKurinMentorAssignmentsQueryHandler : IRequestHandler<GetKurinMentorAssignmentsQuery, ServiceResult<IEnumerable<MentorAssignmentDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetKurinMentorAssignmentsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<IEnumerable<MentorAssignmentDto>>> Handle(GetKurinMentorAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _unitOfWork.MentorAssignments.GetByKurinKeyAsync(request.KurinKey, cancellationToken);
            var memberLookup = (await _unitOfWork.Members.GetMentorCandidatesLookupAsync(request.KurinKey, cancellationToken))
                .Where(member => member.UserKey.HasValue)
                .ToDictionary(member => member.UserKey!.Value);
            var response = new List<MentorAssignmentDto>();

            foreach (var assignment in assignments.OrderByDescending(a => a.AssignedAtUtc))
            {
                response.Add(new MentorAssignmentDto
                {
                    MentorAssignmentKey = assignment.MentorAssignmentKey,
                    MentorUserKey = assignment.MentorUserKey,
                    GroupKey = assignment.GroupKey,
                    GroupName = assignment.Group.Name,
                    AssignedAtUtc = assignment.AssignedAtUtc,
                    RevokedAtUtc = assignment.RevokedAtUtc,
                    Member = memberLookup.GetValueOrDefault(assignment.MentorUserKey)
                });
            }

            return new ServiceResult<IEnumerable<MentorAssignmentDto>>(ResultType.Success, response);
        }
    }
}
