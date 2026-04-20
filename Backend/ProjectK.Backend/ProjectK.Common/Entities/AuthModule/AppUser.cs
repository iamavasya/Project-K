using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Entities.AuthModule
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public Guid? KurinKey { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public OnboardingStatus OnboardingStatus { get; set; }
        public bool IsBetaParticipant { get; set; }
    }

    public enum OnboardingStatus
    {
        RegisteredInactive = 0,
        PendingActivation = 1,
        Active = 2,
        Suspended = 3,
        Archived = 4
    }
}
