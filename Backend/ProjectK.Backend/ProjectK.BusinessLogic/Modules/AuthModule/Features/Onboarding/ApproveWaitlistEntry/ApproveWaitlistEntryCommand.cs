using System;
using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ApproveWaitlistEntry;

public record ApproveWaitlistEntryCommand(Guid WaitlistEntryKey) : IRequest<ServiceResult<Guid>>;
