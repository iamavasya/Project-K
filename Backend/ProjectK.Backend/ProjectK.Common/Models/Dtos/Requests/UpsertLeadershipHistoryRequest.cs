using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos.Requests
{
    public class UpsertLeadershipHistoryRequest
    {
        public Guid LeadershipHistoryKey { get; set; }
        public Guid LeadershipKey { get; set; }
        public Guid MemberKey { get; set; }
        public string Role { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
