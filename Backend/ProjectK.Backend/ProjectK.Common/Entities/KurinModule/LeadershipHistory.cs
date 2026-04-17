using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Entities.KurinModule
{
    public class LeadershipHistory
    {
        public Guid LeadershipHistoryKey { get; set; }

        public Guid MemberKey { get; set; }
        public Member Member { get; set; } = null!;

        public Guid LeadershipKey { get; set; }
        public Leadership Leadership { get; set; } = null!;

        public LeadershipRole Role { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
