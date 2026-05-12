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
    public class GetLeadershipByKey : IRequest<ServiceResult<LeadershipResponse>>
    {
        public Guid LeadershipKey { get; }
        public GetLeadershipByKey(Guid leadershipKey)
        {
            LeadershipKey = leadershipKey;
        }
    }

    public class GetLeadershipByKeyHandler : IRequestHandler<GetLeadershipByKey, ServiceResult<LeadershipResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetLeadershipByKeyHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<LeadershipResponse>> Handle(GetLeadershipByKey request, CancellationToken cancellationToken)
        {
            var leadership = await _unitOfWork.Leaderships.GetByKeyAsync(request.LeadershipKey, cancellationToken);
            if (leadership == null)
            {
                return new ServiceResult<LeadershipResponse>(ResultType.NotFound);
            }
            var LeadershipResponse = _mapper.Map<LeadershipResponse>(leadership);
            switch (LeadershipResponse.Type)
            {
                case LeadershipType.Kurin or LeadershipType.KV:
                    LeadershipResponse.EntityKey = LeadershipResponse.KurinKey ?? Guid.Empty;
                    break;
                case LeadershipType.Group:
                    LeadershipResponse.EntityKey = LeadershipResponse.GroupKey ?? Guid.Empty;
                    break;
            }
            return new ServiceResult<LeadershipResponse>(ResultType.Success, LeadershipResponse);
        }
    }
}
