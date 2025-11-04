using MediatR;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Leaderships
{
    public class GetLeadershipHistoriesQuery : IRequest<ServiceResult<IEnumerable<LeadershipHistoryMemberDto>>>
    {
        public Guid LeadershipKey { get; }
        public GetLeadershipHistoriesQuery(Guid leadershipKey)
        {
            LeadershipKey = leadershipKey;
        }
    }
}
