using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Entities.KurinModule
{
    public class PlastLevelHistory
    {
        public Guid PlastLevelHistoryKey { get; set; }
        
        public Guid MemberKey { get; set; }
        public Member Member { get; set; }

        public PlastLevel PlastLevel { get; set; }

        public DateOnly DateAchieved { get; set; }
    }
}
