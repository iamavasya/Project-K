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

        /// <summary>
        /// Where the level was reached. Nullable because entries recorded before memberships existed
        /// carry no such context — the level is the person's either way.
        /// </summary>
        public Guid? KurinKey { get; set; }
        public Member Member { get; set; }

        public PlastLevel PlastLevel { get; set; }

        public DateOnly DateAchieved { get; set; }
    }
}
