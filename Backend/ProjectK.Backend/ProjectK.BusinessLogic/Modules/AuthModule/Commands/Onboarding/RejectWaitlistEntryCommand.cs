using MediatR;
using ProjectK.Common.Models.Records;
using System;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    public record RejectWaitlistEntryCommand(Guid WaitlistEntryKey, string? Note) : IRequest<ServiceResult<Guid>>;
}
