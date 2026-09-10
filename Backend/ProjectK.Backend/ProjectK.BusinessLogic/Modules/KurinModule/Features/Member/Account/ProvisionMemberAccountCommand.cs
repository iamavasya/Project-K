using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Account
{
    /// <summary>
    /// Gives an existing member the account they sign in with: a pending user, a waitlist entry
    /// recorded as already approved, and the invitation email. Answers with the account's key.
    /// </summary>
    public sealed record ProvisionMemberAccountCommand(Guid MemberKey) : IRequest<ServiceResult<Guid>>;
}
