using ProjectK.Infrastructure.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectK.Common.Entities.AuthModule
{
    public class WaitlistEntry : Entity
    {
        [Key]
        public Guid WaitlistEntryKey { get; set; }
        
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = null!;
        
        [Required, MaxLength(100)]
        public string LastName { get; set; } = null!;
        
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = null!;

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = null!;

        public DateTime DateOfBirth { get; set; }
        
        public bool IsKurinLeaderCandidate { get; set; }
        
        [MaxLength(200)]
        public string? ClaimedKurinNameOrNumber { get; set; }
        
        public WaitlistVerificationStatus VerificationStatus { get; set; }
        
        [MaxLength(100)]
        public string? VerificationChannel { get; set; }
        
        [MaxLength(1000)]
        public string? VerificationNote { get; set; }
        
        public bool IsBetaParticipant { get; set; }
        
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ReviewedByUserKey { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime? InvitationSentAtUtc { get; set; }
        public DateTime? InvitationAcceptedAtUtc { get; set; }
    }

    public enum WaitlistVerificationStatus
    {
        Submitted = 0,
        NeedsManualVerification = 1,
        Verified = 2,
        Rejected = 3,
        ApprovedForInvitation = 4
    }
}
