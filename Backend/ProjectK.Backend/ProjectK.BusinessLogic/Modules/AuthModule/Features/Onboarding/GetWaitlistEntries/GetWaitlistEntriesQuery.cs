using MediatR;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Records;
using System.Collections.Generic;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.GetWaitlistEntries
{
    public record GetWaitlistEntriesQuery : IRequest<ServiceResult<IEnumerable<WaitlistEntry>>>;
}
