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
        public ICollection<Group> Groups { get; set; } = new List<Group>();
        public ICollection<Member> Members { get; set; } = new List<Member>();
    }
}
