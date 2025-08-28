using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Groups.Handlers
{
    public class ExistsGroupByKeyQueryHandler : IRequestHandler<ExistsGroupByKeyQuery, ServiceResult<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public ExistsGroupByKeyQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ServiceResult<bool>> Handle(ExistsGroupByKeyQuery request, CancellationToken cancellationToken)
        {
            var exists = await _unitOfWork.Groups.ExistsAsync(request.GroupKey, cancellationToken);
            return new ServiceResult<bool>(Common.Models.Enums.ResultType.Success, exists);
        }
    }
}
