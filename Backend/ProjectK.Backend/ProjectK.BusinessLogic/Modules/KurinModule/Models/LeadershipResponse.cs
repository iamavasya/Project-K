using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models
{
    public class LeadershipDto
    {
        public Guid LeadershipKey { get; set; }
        public LeadershipType Type { get; set; }
        public Guid EntityKey { get; set; }
        public IEnumerable<LeadershipHistoryMemberDto> LeadershipHistories { get; set; } = new List<LeadershipHistoryMemberDto>();
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}