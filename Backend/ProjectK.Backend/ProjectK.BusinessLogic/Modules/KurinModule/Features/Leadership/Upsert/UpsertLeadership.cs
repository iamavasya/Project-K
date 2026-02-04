using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using LeadershipEntity = ProjectK.Common.Entities.KurinModule.Leadership;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Upsert
{
    public class UpsertLeadership : IRequest<ServiceResult<LeadershipDto>>
    {
        public Guid? LeadershipKey { get; set; }
        public string? Type { get; set; }
        public Guid? EntityKey { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public IEnumerable<LeadershipHistoryMemberDto> LeadershipHistoryMembers { get; set; }

        public UpsertLeadership(UpsertLeadershipRequest request)
        {
            Type = request.Type;
            EntityKey = request.EntityKey;
            StartDate = request.StartDate;
            EndDate = request.EndDate;
            LeadershipHistoryMembers = request.LeadershipHistories;
        }
        public UpsertLeadership(UpsertLeadershipRequest request, Guid leadershipKey)
        {
            LeadershipKey = leadershipKey;
            Type = request.Type;
            EntityKey = request.EntityKey;
            StartDate = request.StartDate;
            EndDate = request.EndDate;
            LeadershipHistoryMembers = request.LeadershipHistories;
        }
    }

    public class UpsertLeadershipHandler : IRequestHandler<UpsertLeadership, ServiceResult<LeadershipDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpsertLeadershipHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<LeadershipDto>> Handle(UpsertLeadership request, CancellationToken cancellationToken)
        {
            LeadershipEntity? existing = null;
            bool isCreated = false;
            if (request.LeadershipKey != null && request.LeadershipKey != Guid.Empty)
            {
                existing = await _unitOfWork.Leaderships.GetByKeyAsync(request.LeadershipKey!.Value, cancellationToken);
            }

            if (existing != null)
            {
                // Update existing Leadership
                _mapper.Map(request, existing);

                existing.Type = Enum.Parse<Common.Models.Enums.LeadershipType>(request.Type!, ignoreCase: true);
                switch (existing.Type)
                {
                    case LeadershipType.Kurin or LeadershipType.KV:
                        existing.KurinKey = request.EntityKey;
                        existing.GroupKey = null;
                        break;
                    case LeadershipType.Group:
                        existing.GroupKey = request.EntityKey;
                        existing.KurinKey = null;
                        break;
                }
                _unitOfWork.Leaderships.Update(existing, cancellationToken);
            }
            else
            {
                // Create new Leadership
                existing = _mapper.Map<LeadershipEntity>(request);
                existing.Type = Enum.Parse<Common.Models.Enums.LeadershipType>(request.Type!, ignoreCase: true);
                switch (existing.Type)
                {
                    case LeadershipType.Kurin or LeadershipType.KV:
                        existing.KurinKey = request.EntityKey;
                        existing.GroupKey = null;
                        break;
                    case LeadershipType.Group:
                        existing.GroupKey = request.EntityKey;
                        existing.KurinKey = null;
                        break;
                }
                _unitOfWork.Leaderships.Add(existing, cancellationToken);
                isCreated = true;
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<LeadershipDto>(ResultType.InternalServerError);
            }

            var response = _mapper.Map<LeadershipDto>(existing);

            return isCreated
                ? new ServiceResult<LeadershipDto>(ResultType.Created, response, CreatedAtActionName: "GetLeadershipByKey", CreatedAtRouteValues: new { leadershipKey = response.LeadershipKey })
                : new ServiceResult<LeadershipDto>(ResultType.Success, response);
        }
    }
}
