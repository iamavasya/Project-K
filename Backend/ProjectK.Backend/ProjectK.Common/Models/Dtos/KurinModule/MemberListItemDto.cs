using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;

namespace ProjectK.Common.Models.Dtos.KurinModule
{
    // Lean read model for member-list screens. Projected straight from the query
    // (no Include graph): the list needs identity, account role, level, photo,
    // verification, plus only the *active* leadership roles and warnings. Full
    // level history and awards are member-card concerns and are left out here.
    public class MemberListItemDto
    {
        public Guid MemberKey { get; set; }
        public Guid? GroupKey { get; set; }

        /// <summary>
        /// The гурток's name. The key alone was enough while the list only ever linked to a гурток;
        /// the registry prints it, and prints it for a whole kurin at once.
        /// </summary>
        public string? GroupName { get; set; }
        public Guid KurinKey { get; set; }
        public Guid? UserKey { get; set; }
        public string? UserRole { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? School { get; set; }
        public PlastLevel? LatestPlastLevel { get; set; }
        public string? ProfilePhotoBlobName { get; set; }
        public MemberProfileVerificationStatus ProfileVerificationStatus { get; set; }
        public DateTime? ProfileVerifiedAtUtc { get; set; }
        public Guid? ProfileVerifiedByUserKey { get; set; }
        public string? ProfileVerificationNote { get; set; }
        /// <summary>
        /// Every ступінь this person has reached, with the date. Read here — and not only on the
        /// member's own card — because the registry is a column per ступінь across the whole kurin,
        /// and asking per person would be one query each.
        /// </summary>
        public List<PlastLevelHistoryDto> PlastLevelHistories { get; set; } = [];

        public List<LeadershipHistoryDto> LeadershipHistories { get; set; } = [];
        public List<MemberWarningDto> Warnings { get; set; } = [];
    }

    // Field-level visibility for a member list, computed once from the caller's
    // identity and passed into the projection so Address/School are masked in SQL
    // instead of being read and then scrubbed in memory.
    public sealed record MemberFieldVisibility(
        bool CanSeeAllPrivate,
        Guid? CurrentUserId,
        IReadOnlyCollection<Guid> VisibleGroupKeys);
}
