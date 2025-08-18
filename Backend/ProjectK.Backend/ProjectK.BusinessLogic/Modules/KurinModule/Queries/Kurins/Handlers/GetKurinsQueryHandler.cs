using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Kurins.Handlers
{
    public class GetKurinsQueryHandler : IRequestHandler<GetKurinsQuery, ServiceResult<IEnumerable<KurinResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetKurinsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<KurinResponse>>> Handle(GetKurinsQuery request, CancellationToken cancellationToken)
        {
            var kurins = await _unitOfWork.Kurins.GetAllAsync(cancellationToken);
            var kurinResponses = _mapper.Map<IEnumerable<KurinResponse>>(kurins);
            return new ServiceResult<IEnumerable<KurinResponse>>(ResultType.Success, kurinResponses);
        }
    }
}
