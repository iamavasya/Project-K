using MediatR;
using ProjectK.Common.Models.Records;
using System;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ResendInvitation
{
    public record ResendInvitationCommand(Guid WaitlistEntryKey) : IRequest<ServiceResult<Guid>>;

    public record ResendInvitationByEmailCommand(string Email) : IRequest<ServiceResult<bool>>;
}
