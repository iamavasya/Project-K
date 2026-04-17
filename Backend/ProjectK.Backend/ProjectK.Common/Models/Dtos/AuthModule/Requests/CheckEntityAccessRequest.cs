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

        // Kept for backward compatibility with existing clients.
        // Backend ignores this value for security decisions and uses claims/context scope only.
        public string? ActiveKurinKey { get; set; }
    }
}
