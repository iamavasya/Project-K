using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Services.Caching;
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
    public class GetGroups : IRequest<ServiceResult<IEnumerable<GroupResponse>>>
    {
        public Guid KurinKey { get; set; }
        public GetGroups(Guid kurinKey)
        {
            KurinKey = kurinKey;
        }
    }

    public class GetGroupsHandler : IRequestHandler<GetGroups, ServiceResult<IEnumerable<GroupResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IBackendCache _cache;

        public GetGroupsHandler(IUnitOfWork unitOfWork, IMapper mapper, IBackendCache cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ServiceResult<IEnumerable<GroupResponse>>> Handle(GetGroups request, CancellationToken cancellationToken)
        {
            return await _cache.GetOrCreateAsync(
                BackendCachePolicies.GroupReads,
                $"list:kurin:{request.KurinKey}",
                async token =>
                {
                    var groups = await _unitOfWork.Groups.GetAllAsync(request.KurinKey, token);
                    var response = _mapper.Map<IEnumerable<GroupResponse>>(groups).ToList();
                    return new ServiceResult<IEnumerable<GroupResponse>>(ResultType.Success, response);
                },
                cancellationToken);
        }
    }
}
