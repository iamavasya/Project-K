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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Get
{
    public class GetKurins : IRequest<ServiceResult<IEnumerable<KurinResponse>>>
    {
    }

    public class GetKurinsHandler : IRequestHandler<GetKurins, ServiceResult<IEnumerable<KurinResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetKurinsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<KurinResponse>>> Handle(GetKurins request, CancellationToken cancellationToken)
        {
            var kurins = await _unitOfWork.Kurins.GetAllAsync(cancellationToken);
            var kurinResponses = _mapper.Map<IEnumerable<KurinResponse>>(kurins);
            return new ServiceResult<IEnumerable<KurinResponse>>(ResultType.Success, kurinResponses);
        }
    }
}
