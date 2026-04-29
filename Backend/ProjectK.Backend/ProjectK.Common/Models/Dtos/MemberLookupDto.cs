using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos
{
    public class MemberLookupDto
    {
        public Guid MemberKey { get; set; }
        public Guid? UserKey { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
    }
}
