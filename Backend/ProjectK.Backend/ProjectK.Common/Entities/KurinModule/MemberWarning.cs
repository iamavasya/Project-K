using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.Entities;
using System;

namespace ProjectK.Common.Entities.KurinModule
{
    public class MemberWarning : Entity
    {
        public Guid MemberWarningKey { get; set; } = Guid.NewGuid();
        public Guid MemberKey { get; set; }
        public Member Member { get; set; } = null!;
        public MemberWarningLevel Level { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public Guid IssuedByUserKey { get; set; }
        public Guid? RevokedByUserKey { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
    }
}
