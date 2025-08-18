using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models
{
    public class GroupResponse
    {
        public Guid GroupKey { get; set; }
        public Guid KurinKey { get; set; }
        public string Name { get; set; }
        public int KurinNumber { get; set; }
    }
}
