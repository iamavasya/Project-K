using ProjectK.Common.Entities.AuthModule;
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
        public string? ProfilePhotoBlobName { get; set; }
        public Group? Group { get; set; }
        public Kurin Kurin { get; set; }
        public AppUser? User { get; set; }
    }
}
