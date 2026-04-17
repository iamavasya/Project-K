using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Get
{
    public class GetGroupByKey : IRequest<ServiceResult<GroupResponse>>
    {
        public Guid GroupKey { get; set; }
        public GetGroupByKey(Guid groupKey)
        {
            GroupKey = groupKey;
        }
    }

    public class GetGroupByKeyHandler : IRequestHandler<GetGroupByKey, ServiceResult<GroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetGroupByKeyHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<GroupResponse>> Handle(GetGroupByKey request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey, cancellationToken);

            if (group is null)
            {
                return new ServiceResult<GroupResponse>(ResultType.NotFound);
            }

            var response = _mapper.Map<GroupResponse>(group);

            return new ServiceResult<GroupResponse>(ResultType.Success, response);
        }
    }
}
