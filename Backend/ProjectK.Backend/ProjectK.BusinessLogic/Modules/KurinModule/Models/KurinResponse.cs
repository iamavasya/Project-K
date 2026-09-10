using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models
{
    public class KurinResponse
    {
        public Guid KurinKey { get; set; }
        public int Number { get; set; }

        /// <summary>Яка гілка. Read by the UI, which shows юнацькі речі only where they mean something.</summary>
        public KurinBranch Branch { get; set; }
        public string? Stanytsia { get; set; }
        public string? RegionOrCountry { get; set; }
        public string? NamedAfter { get; set; }
        public string? Description { get; set; }
        public bool IsZbtEnabled { get; set; }
        public int ZbtUserCap { get; set; }
        public int CurrentUserCount { get; set; }
        public bool ProfileVerificationEnabled { get; set; }
    }
}
