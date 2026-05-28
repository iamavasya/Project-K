using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models
{
    public class KurinResponse
    {
        public Guid KurinKey { get; set; }
        public int Number { get; set; }
        public string? Name { get; set; }
        public string? Stanytsia { get; set; }
        public string? RegionOrCountry { get; set; }
        public string? NamedAfter { get; set; }
        public string? Description { get; set; }
        public bool IsZbtEnabled { get; set; }
        public int ZbtUserCap { get; set; }
        public int CurrentUserCount { get; set; }
    }
}
