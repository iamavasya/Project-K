using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.Common.Entities;

namespace ProjectK.Common.Entities.KurinModule
{
    /// <summary>
    /// A person. Not a person <i>of</i> anywhere — where they belong is said by
    /// <see cref="Membership"/>, one row per kurin, and a person may hold several at once or none at
    /// all. Everything here is theirs and outlives any of those: leaving a kurin, or the kurin
    /// itself being deleted, leaves this record and its history untouched.
    /// </summary>
    public class Member : Entity
    {
        public Guid MemberKey { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The code this person shares so another kurin can find them. Derived from
        /// <see cref="MemberKey"/> — see <c>MemberPublicId</c>.
        /// </summary>
        public string PublicId { get; set; } = string.Empty;
        public Guid? UserKey { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? School { get; set; }
        public string? ProfilePhotoBlobName { get; set; }
        public MemberProfileVerificationStatus ProfileVerificationStatus { get; set; } = MemberProfileVerificationStatus.Unverified;
        public DateTime? ProfileVerifiedAtUtc { get; set; }
        public Guid? ProfileVerifiedByUserKey { get; set; }
        public string? ProfileVerificationNote { get; set; }
        public PlastLevel? LatestPlastLevel { get; set; }
        public ICollection<PlastLevelHistory> PlastLevelHistory { get; set; } = new List<PlastLevelHistory>();
        public ICollection<LeadershipHistory> LeadershipHistories { get; set; } = new List<LeadershipHistory>();
        public ICollection<MemberWarning> MemberWarnings { get; set; } = new List<MemberWarning>();
        public ICollection<MemberAward> MemberAwards { get; set; } = new List<MemberAward>();
        public AppUser? User { get; set; }
    }
}
