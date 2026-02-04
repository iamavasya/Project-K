using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Get
{
    public class ExistsGroupByKey(Guid groupKey) : IRequest<ServiceResult<bool>>
    {
        public Guid GroupKey { get; set; } = groupKey;
    }

    public class ExistsGroupByKeyHandler : IRequestHandler<ExistsGroupByKey, ServiceResult<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public ExistsGroupByKeyHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ServiceResult<bool>> Handle(ExistsGroupByKey request, CancellationToken cancellationToken)
        {
            var exists = await _unitOfWork.Groups.ExistsAsync(request.GroupKey, cancellationToken);
            return new ServiceResult<bool>(Common.Models.Enums.ResultType.Success, exists);
        }
    }
}
