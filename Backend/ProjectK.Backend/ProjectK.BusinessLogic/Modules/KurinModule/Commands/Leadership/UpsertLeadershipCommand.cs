using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Leadership
{
    public class UpsertLeadershipCommand : IRequest<ServiceResult<LeadershipDto>>
    {
        public Guid? LeadershipKey { get; set; }
        public string? Type { get; set; }
        public Guid? EntityKey { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public IEnumerable<LeadershipHistoryMemberDto> LeadershipHistoryMembers { get; set; }

        public UpsertLeadershipCommand(UpsertLeadershipRequest request)
        {
            Type = request.Type;
            EntityKey = request.EntityKey;
            StartDate = request.StartDate;
            EndDate = request.EndDate;
            LeadershipHistoryMembers = request.LeadershipHistories;
        }

        public UpsertLeadershipCommand(UpsertLeadershipRequest request, Guid leadershipKey)
        {
            LeadershipKey = leadershipKey;
            Type = request.Type;
            EntityKey = request.EntityKey;
            StartDate = request.StartDate;
            EndDate = request.EndDate;
            LeadershipHistoryMembers = request.LeadershipHistories;
        }
    }
}
