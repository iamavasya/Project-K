using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Leaderships.Handlers
{
    public class GetLeadershipQueryHandler : IRequestHandler<GetLeadershipQuery, ServiceResult<IEnumerable<MemberResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLeadershipQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<MemberResponse>>> Handle(GetLeadershipQuery request, CancellationToken cancellationToken)
        {
            var leadership = await _unitOfWork.Leaderships.GetAllByTypeAsync(request.LeadershipType, request.TypeKey, cancellationToken);
            var leadershipMembers = leadership
                                        .SelectMany(l => l.LeadershipHistories)
                                        .Where(h => h.EndDate == null || h.EndDate > DateOnly.FromDateTime(DateTime.UtcNow))
                                        .Select(h => h.Member)
                                        .Distinct()
                                        .ToList();
            var memberResponses = _mapper.Map<IEnumerable<MemberResponse>>(leadershipMembers);
            return new ServiceResult<IEnumerable<MemberResponse>>(ResultType.Success, memberResponses);
        }
    }
}
