using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Entities.KurinModule
{
    public class Kurin(int number) : Entity
    {
        public Guid KurinKey { get; set; } = Guid.NewGuid();
        public int Number { get; set; } = number;
        public int ZbtUserCap { get; set; } = 10;
        public bool IsZbtKurin { get; set; }
        public ICollection<Group> Groups { get; set; } = new List<Group>();
        public ICollection<Member> Members { get; set; } = new List<Member>();
        public ICollection<Leadership> Leaderships { get; set; } = new List<Leadership>();
        public ICollection<PlanningSession> PlanningSessions { get; set; } = new List<PlanningSession>();
    }
}
