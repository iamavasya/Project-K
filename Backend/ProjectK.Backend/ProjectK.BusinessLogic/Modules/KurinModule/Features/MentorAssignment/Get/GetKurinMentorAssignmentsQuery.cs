using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment.Get
{
    public record GetKurinMentorAssignmentsQuery(Guid KurinKey) : IRequest<ServiceResult<IEnumerable<MentorAssignmentDto>>>;

    public class GetKurinMentorAssignmentsQueryHandler : IRequestHandler<GetKurinMentorAssignmentsQuery, ServiceResult<IEnumerable<MentorAssignmentDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemberDirectory _members;

        public GetKurinMentorAssignmentsQueryHandler(IUnitOfWork unitOfWork, IMemberDirectory members)
        {
            _unitOfWork = unitOfWork;
            _members = members;
        }

        public async Task<ServiceResult<IEnumerable<MentorAssignmentDto>>> Handle(GetKurinMentorAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _unitOfWork.MentorAssignments.GetByKurinKeyAsync(request.KurinKey, cancellationToken);
            var memberLookup = (await _members.GetLookupByKurinAsync(request.KurinKey, cancellationToken))
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
