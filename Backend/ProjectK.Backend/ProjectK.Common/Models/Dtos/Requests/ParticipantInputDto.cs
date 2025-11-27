using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Models.Dtos.Requests
{
    public class ParticipantInputDto
    {
        public Guid MemberKey { get; set; }
        public string FullName { get; set; }
        public double RoleWeight { get; set; }
        public List<DateRangeDto> BusyRanges { get; set; } = [];
    }
}
