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
    public class GetLeadershipByKey : IRequest<ServiceResult<LeadershipDto>>
    {
        public Guid LeadershipKey { get; }
        public GetLeadershipByKey(Guid leadershipKey)
        {
            LeadershipKey = leadershipKey;
        }
    }

    public class GetLeadershipByKeyHandler : IRequestHandler<GetLeadershipByKey, ServiceResult<LeadershipDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetLeadershipByKeyHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<LeadershipDto>> Handle(GetLeadershipByKey request, CancellationToken cancellationToken)
        {
            var leadership = await _unitOfWork.Leaderships.GetByKeyAsync(request.LeadershipKey, cancellationToken);
            if (leadership == null)
            {
                return new ServiceResult<LeadershipDto>(ResultType.NotFound);
            }
            var leadershipDto = _mapper.Map<LeadershipDto>(leadership);
            switch (leadershipDto.Type)
            {
                case LeadershipType.Kurin or LeadershipType.KV:
                    leadershipDto.EntityKey = leadershipDto.KurinKey ?? Guid.Empty;
                    break;
                case LeadershipType.Group:
                    leadershipDto.EntityKey = leadershipDto.GroupKey ?? Guid.Empty;
                    break;
            }
            return new ServiceResult<LeadershipDto>(ResultType.Success, leadershipDto);
        }
    }
}
