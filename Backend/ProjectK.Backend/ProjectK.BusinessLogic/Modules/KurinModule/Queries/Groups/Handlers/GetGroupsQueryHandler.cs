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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Groups.Handlers
{
    public class GetGroupsQueryHandler : IRequestHandler<GetGroupsQuery, ServiceResult<IEnumerable<GroupResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetGroupsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<GroupResponse>>> Handle(GetGroupsQuery request, CancellationToken cancellationToken)
        {
            var groups = await _unitOfWork.Groups.GetAllAsync(request.KurinKey, cancellationToken);
            var response = _mapper.Map<IEnumerable<GroupResponse>>(groups);
            return new ServiceResult<IEnumerable<GroupResponse>>(ResultType.Success, response);
        }
    }
}
