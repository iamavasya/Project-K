using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos
{
    public class LeadershipHistoryMemberDto
    {
        public Guid? LeadershipHistoryKey { get; set; }
        public Guid? LeadershipKey { get; set; }
        public string Role { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // Member Details
        public MemberLookupDto Member { get; set; }
    }
}
