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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Get
{
    public class GetKurins : IRequest<ServiceResult<IEnumerable<KurinResponse>>>
    {
    }

    public class GetKurinsHandler : IRequestHandler<GetKurins, ServiceResult<IEnumerable<KurinResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IBackendCache _cache;

        public GetKurinsHandler(IUnitOfWork unitOfWork, IMapper mapper, IBackendCache cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ServiceResult<IEnumerable<KurinResponse>>> Handle(GetKurins request, CancellationToken cancellationToken)
        {
            return await _cache.GetOrCreateAsync(
                BackendCachePolicies.KurinReads,
                "list",
                async token =>
                {
                    var kurins = await _unitOfWork.Kurins.GetAllAsync(token);
                    var kurinResponses = _mapper.Map<IEnumerable<KurinResponse>>(kurins).ToList();
                    return new ServiceResult<IEnumerable<KurinResponse>>(ResultType.Success, kurinResponses);
                },
                cancellationToken);
        }
    }
}
