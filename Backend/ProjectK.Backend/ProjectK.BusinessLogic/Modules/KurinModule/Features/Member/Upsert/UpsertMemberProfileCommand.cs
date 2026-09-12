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
    public ICollection<PlastLevelHistoryDto> PlastLevelHistories { get; set; } = [];
}
