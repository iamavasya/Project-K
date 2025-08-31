using ProjectK.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Entities.KurinModule
{
    public class Group : Entity
    {
        public Guid GroupKey { get; set; } = Guid.NewGuid();
        public Guid KurinKey { get; set; }
        public string Name { get; set; }
        public Kurin Kurin { get; set; }
        public ICollection<Member> Members { get; set; } = new List<Member>();

        public Group(string name, Guid kurinKey)
        {
            Name = name;
            KurinKey = kurinKey;
        }
    }
}
