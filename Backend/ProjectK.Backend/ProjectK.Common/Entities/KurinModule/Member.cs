using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule.Leadership;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Entities.KurinModule
{
    public class Member : Entity
    {
        public Guid MemberKey { get; set; } = Guid.NewGuid();
        public Guid? GroupKey { get; set; }
        public Guid KurinKey { get; set; }
        public Guid? UserKey { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? School { get; set; }
        public string? ProfilePhotoBlobName { get; set; }
        public Kurin Kurin { get; set; }
        public Group? Group { get; set; }
        public PlastLevel? LatestPlastLevel { get; set; }
        public ICollection<PlastLevelHistory> PlastLevelHistory { get; set; } = new List<PlastLevelHistory>();
        public ICollection<LeadershipHistory> LeadershipHistories { get; set; } = new List<LeadershipHistory>();
        public AppUser? User { get; set; }
    }
}
