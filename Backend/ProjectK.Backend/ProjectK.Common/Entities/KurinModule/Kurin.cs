using ProjectK.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Entities.KurinModule
{
    public class Kurin : Entity
    {
        public Guid KurinKey { get; set; }
        public int Number { get; set; }
    }
}
