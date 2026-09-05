using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos.KurinModule
{
    public class PlastLevelHistoryDto
    {
        public Guid? MemberKey { get; set; }
        public Guid? PlastLevelHistoryKey { get; set; }
        public PlastLevel PlastLevel { get; set; }
        public DateOnly DateAchieved { get; set; }
    }
}
