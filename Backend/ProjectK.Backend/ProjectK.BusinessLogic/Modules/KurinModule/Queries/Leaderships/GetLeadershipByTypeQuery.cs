using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Leaderships
{
    public class GetLeadershipByTypeQuery : IRequest<ServiceResult<LeadershipDto>>
    {
        public LeadershipType LeadershipType { get; }
        public Guid TypeKey { get; }
        public GetLeadershipByTypeQuery(string leadershipType, Guid typeKey)
        {
            LeadershipType = Enum.Parse<LeadershipType>(leadershipType, true);
            TypeKey = typeKey;
        }
    }
}
