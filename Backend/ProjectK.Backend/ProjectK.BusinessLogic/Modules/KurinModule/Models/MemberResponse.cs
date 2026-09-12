using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models;

public class MemberResponse
{
    public Guid MemberKey { get; set; }

    /// <summary>
    /// The code a person hands to another kurin's провід so it can take them in. Only ever filled
    /// in for the person themselves — see <c>GetMemberByKeyQuery</c>, which clears it for everyone else.
    /// </summary>
    public string? PublicId { get; set; }
    public Guid GroupKey { get; set; }

    /// <summary>
    /// The гурток's name, when the read knew it. Null from reads that answer about one person
    /// rather than a placement — the card already shows the гурток from the membership block.
    /// </summary>
    public string? GroupName { get; set; }
    public Guid KurinKey { get; set; }

    /// <summary>
    /// Whether they are в кадрі виховників of the kurin this read was about. Filled by list
    /// reads; a read about one person leaves it false, because standing is a property of a
    /// placement and there is no placement in view.
    /// </summary>
    public bool IsStaff { get; set; }

    /// <summary>The гуртки they run as виховник in that kurin, by name. Empty for a юнак.</summary>
    public ICollection<string> MentoredGroupNames { get; set; } = [];
    public Guid? UserKey { get; set; }
    public string? UserRole { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? School { get; set; }
    public PlastLevel? LatestPlastLevel { get; set; }
    public ICollection<PlastLevelHistoryDto> PlastLevelHistories { get; set; } = [];
    public ICollection<LeadershipHistoryDto> LeadershipHistories { get; set; } = [];
    public ICollection<MemberWarningDto> Warnings { get; set; } = [];
    public ICollection<MemberAwardDto> Awards { get; set; } = [];
    public string? ProfilePhotoUrl { get; set; }
    public MemberProfileVerificationStatus ProfileVerificationStatus { get; set; }
    public DateTime? ProfileVerifiedAtUtc { get; set; }
    public Guid? ProfileVerifiedByUserKey { get; set; }
    public string? ProfileVerificationNote { get; set; }
}
