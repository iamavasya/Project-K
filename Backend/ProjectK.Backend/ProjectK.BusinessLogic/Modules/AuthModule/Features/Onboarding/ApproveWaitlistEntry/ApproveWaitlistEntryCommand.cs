using MediatR;
using ProjectK.Common.Models.Records;
using System;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ApproveWaitlistEntry
{
    public record ApproveWaitlistEntryCommand(Guid WaitlistEntryKey) : IRequest<ServiceResult<Guid>>;
}
