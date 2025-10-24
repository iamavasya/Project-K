using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Leadership;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models
{
    public class MemberResponse
    {
        public Guid MemberKey { get; set; }
        public Guid GroupKey { get; set; }
        public Guid KurinKey { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? School { get; set; }
        public PlastLevel? LatestPlastLevel { get; set; }
        public ICollection<PlastLevelHistoryDto> PlastLevelHistories { get; set; } = [];
        public ICollection<LeadershipHistoryDto> LeadershipHistories { get; set; } = [];
        public string? ProfilePhotoUrl { get; set; }
    }
}
