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
    public class GetLeadershipByKeyQueryHandler : IRequestHandler<GetLeadershipByKeyQuery, ServiceResult<LeadershipDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetLeadershipByKeyQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<LeadershipDto>> Handle(GetLeadershipByKeyQuery request, CancellationToken cancellationToken)
        {
            var leadership = await _unitOfWork.Leaderships.GetByKeyAsync(request.LeadershipKey, cancellationToken);
            if (leadership == null)
            {
                return new ServiceResult<LeadershipDto>(ResultType.NotFound);
            }
            var leadershipDto = _mapper.Map<LeadershipDto>(leadership);
            return new ServiceResult<LeadershipDto>(ResultType.Success, leadershipDto);
        }
    }
}
