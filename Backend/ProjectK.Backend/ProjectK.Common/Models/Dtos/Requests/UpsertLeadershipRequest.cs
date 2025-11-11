using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos.Requests
{
    public class UpsertLeadershipRequest
    {
        public Guid? LeadershipKey { get; set; }
        public string? Type { get; set; }
        public Guid? EntityKey { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public IEnumerable<LeadershipHistoryMemberDto> LeadershipHistoryMembers { get; set; } = [];
    }
}
