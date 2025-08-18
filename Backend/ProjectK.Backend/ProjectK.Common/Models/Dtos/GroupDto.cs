using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos
{
    public class GroupDto
    {
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public Guid KurinKey { get; set; }
    }
}
