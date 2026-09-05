using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Entities.KurinModule.Planning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.Common.Entities;

namespace ProjectK.Common.Entities.KurinModule
{
    public class Kurin(int number) : Entity
    {
        public Guid KurinKey { get; set; } = Guid.NewGuid();
        public int Number { get; set; } = number;
        public string? Stanytsia { get; set; }
        public string? RegionOrCountry { get; set; }
        public string? NamedAfter { get; set; }
        public string? Description { get; set; }

        /// <summary>Which пластова гілка this kurin belongs to.</summary>
        public KurinBranch Branch { get; set; } = KurinBranch.UPYu;
        public int ZbtUserCap { get; set; } = 15;
        public bool IsZbtKurin { get; set; }
        public bool ProfileVerificationEnabled { get; set; }
        public ICollection<Group> Groups { get; set; } = new List<Group>();
        public ICollection<Member> Members { get; set; } = new List<Member>();
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public ICollection<Leadership> Leaderships { get; set; } = new List<Leadership>();
        public ICollection<PlanningSession> PlanningSessions { get; set; } = new List<PlanningSession>();
        public ICollection<AgendaItem> AgendaItems { get; set; } = new List<AgendaItem>();
    }
}
