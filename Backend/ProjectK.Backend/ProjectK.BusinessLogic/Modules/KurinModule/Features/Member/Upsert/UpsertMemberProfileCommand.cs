using MediatR;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;

/// <summary>
/// The member's own data — who they are and where they are placed. Carries no photo and no
/// account: those are separate use cases, so this one owns exactly the member row.
/// </summary>
public class UpsertMemberProfileCommand : IRequest<ServiceResult<MemberProfileWriteResult>>
{
    public Guid MemberKey { get; set; }
    public Guid? KurinKey { get; set; }
    public Guid? GroupKey { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? School { get; set; }
    public string? MotherFirstName { get; set; }
    public string? MotherLastName { get; set; }
    public string? MotherPhone { get; set; }
    public string? FatherFirstName { get; set; }
    public string? FatherLastName { get; set; }
    public string? FatherPhone { get; set; }
    public ICollection<PlastLevelHistoryDto> PlastLevelHistories { get; set; } = [];

    /// <summary>Values of the край's own card fields, by key. A key left out keeps its value.</summary>
    public IDictionary<string, string>? CardExtras { get; set; }

    /// <summary>
    /// Holds the write to the card fields of the member's край: required ones filled, hidden ones
    /// left as they are. The forms ask for it; the roster import and kurin registration do not, so
    /// a край's new required field does not turn away a whole import.
    /// </summary>
    public bool ApplyCardFields { get; set; }
}
