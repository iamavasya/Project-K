using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Entities.KurinModule
{
    public class Leadership
    {
        public Guid LeadershipKey { get; set; }
        public LeadershipType Type { get; set; }
        public Guid EntityKey { get; set; }
        public string? Name { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public ICollection<LeadershipHistory> LeadershipHistories { get; set; } = new List<LeadershipHistory>();
    }
}
