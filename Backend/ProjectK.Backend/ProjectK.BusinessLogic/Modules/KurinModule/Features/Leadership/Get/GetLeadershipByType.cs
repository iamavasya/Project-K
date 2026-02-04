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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Get
{
    public class GetLeadershipByType : IRequest<ServiceResult<LeadershipDto>>
    {
        public LeadershipType LeadershipType { get; }
        public Guid TypeKey { get; }
        public GetLeadershipByType(string leadershipType, Guid typeKey)
        {
            LeadershipType = Enum.Parse<LeadershipType>(leadershipType, true);
            TypeKey = typeKey;
        }
    }

    public class GetLeadershipByTypeHandler : IRequestHandler<GetLeadershipByType, ServiceResult<LeadershipDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLeadershipByTypeHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<LeadershipDto>> Handle(GetLeadershipByType request, CancellationToken cancellationToken)
        {
            var leadership = await _unitOfWork.Leaderships.GetAllByTypeAsync(request.LeadershipType, request.TypeKey, cancellationToken);
            var currentLeadership = leadership.Where(l => l.EndDate == null);
            if (leadership.Count() > 1)
            {
                throw new ArgumentOutOfRangeException("Multiple leaderships found for the given type and key.");
            }
            LeadershipDto leadershipResponse = _mapper.Map<LeadershipDto>(currentLeadership.FirstOrDefault());
            return new ServiceResult<LeadershipDto>(
                ResultType.Success,
                leadershipResponse);
        }
    }
}
