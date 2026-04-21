using MediatR;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Records;
using System.Collections.Generic;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Onboarding
{
    public record GetWaitlistEntriesQuery : IRequest<ServiceResult<IEnumerable<WaitlistEntry>>>;
}
