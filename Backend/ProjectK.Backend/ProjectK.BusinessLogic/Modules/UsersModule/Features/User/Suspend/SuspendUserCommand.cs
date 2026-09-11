using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.User.Suspend
{
    /// <summary>Refuses the account at sign-in and ends every session it holds. Reversible.</summary>
    public record SuspendUserCommand(Guid TargetUserKey) : IRequest<ServiceResult<bool>>;

    /// <summary>Lets a suspended account sign in again. Sessions are not restored; the person signs in anew.</summary>
    public record RestoreUserCommand(Guid TargetUserKey) : IRequest<ServiceResult<bool>>;
}
