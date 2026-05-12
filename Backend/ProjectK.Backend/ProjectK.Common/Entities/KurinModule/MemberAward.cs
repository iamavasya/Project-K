using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.Entities;
using System;

namespace ProjectK.Common.Entities.KurinModule
{
    public class MemberAward : Entity
    {
        public Guid MemberAwardKey { get; set; } = Guid.NewGuid();
        public Guid MemberKey { get; set; }
        public Guid KurinKey { get; set; }
        public Member Member { get; set; } = null!;
        public MemberAwardLevel Level { get; set; }
        public DateTime DateAcquired { get; set; }
        public string? Note { get; set; }
        public BadgeProgressStatus Status { get; set; } = BadgeProgressStatus.Confirmed;
        public DateTime? SubmittedAtUtc { get; set; }
        public Guid? SubmittedByUserKey { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ReviewedByUserKey { get; set; }
    }
}
