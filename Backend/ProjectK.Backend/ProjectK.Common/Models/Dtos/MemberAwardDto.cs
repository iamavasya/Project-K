using ProjectK.Common.Models.Enums;
using System;

namespace ProjectK.Common.Models.Dtos
{
    public class MemberAwardDto
    {
        public Guid MemberAwardKey { get; set; }
        public Guid MemberKey { get; set; }
        public Guid KurinKey { get; set; }
        public MemberAwardLevel Level { get; set; }
        public DateTime DateAcquired { get; set; }
        public string? Note { get; set; }
        public BadgeProgressStatus Status { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
        public Guid? SubmittedByUserKey { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ReviewedByUserKey { get; set; }
    }
}
