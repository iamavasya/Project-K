using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Get
{
    public class GetLeadershipHistories : IRequest<ServiceResult<IEnumerable<LeadershipHistoryMemberDto>>>
    {
        public Guid LeadershipKey { get; }
        public GetLeadershipHistories(Guid leadershipKey)
        {
            LeadershipKey = leadershipKey;
        }
    }

    public class GetLeadershipHistoriesHandler : IRequestHandler<GetLeadershipHistories, ServiceResult<IEnumerable<LeadershipHistoryMemberDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetLeadershipHistoriesHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<ServiceResult<IEnumerable<LeadershipHistoryMemberDto>>> Handle(GetLeadershipHistories request, CancellationToken cancellationToken)
        {
            var histories = await _unitOfWork.Leaderships.GetLeadershipHistoriesAsync(request.LeadershipKey, cancellationToken);
            var historyDtos = _mapper.Map<IEnumerable<LeadershipHistoryMemberDto>>(histories);
            return new ServiceResult<IEnumerable<LeadershipHistoryMemberDto>>(ResultType.Success, historyDtos);
        }
    }
}
