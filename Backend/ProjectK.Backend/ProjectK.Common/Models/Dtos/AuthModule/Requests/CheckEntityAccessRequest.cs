using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos.AuthModule.Requests
{
    public class CheckEntityAccessRequest
    {
        public string EntityType { get; set; }
        public string EntityKey { get; set; }
        public string? ActiveKurinKey { get; set; }
    }
}
