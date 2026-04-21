using MediatR;
using ProjectK.Common.Models.Records;
using System;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    public record ResendInvitationCommand(Guid WaitlistEntryKey) : IRequest<ServiceResult<Guid>>;
}
