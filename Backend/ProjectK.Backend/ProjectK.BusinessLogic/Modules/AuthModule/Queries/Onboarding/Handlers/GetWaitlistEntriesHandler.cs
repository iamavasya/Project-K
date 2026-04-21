using MediatR;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Onboarding.Handlers
{
    public class GetWaitlistEntriesHandler : IRequestHandler<GetWaitlistEntriesQuery, ServiceResult<IEnumerable<WaitlistEntry>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetWaitlistEntriesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<IEnumerable<WaitlistEntry>>> Handle(GetWaitlistEntriesQuery request, CancellationToken cancellationToken)
        {
            var entries = await _unitOfWork.WaitlistEntries.GetAllAsync(cancellationToken);
            return new ServiceResult<IEnumerable<WaitlistEntry>>(ResultType.Success, entries);
        }
    }
}
