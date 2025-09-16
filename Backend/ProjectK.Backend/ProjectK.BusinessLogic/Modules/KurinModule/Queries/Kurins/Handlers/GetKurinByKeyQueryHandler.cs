using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Kurins.Handlers
{
    public class GetKurinByKeyQueryHandler : IRequestHandler<GetKurinByKeyQuery, ServiceResult<KurinResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetKurinByKeyQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<KurinResponse>> Handle(GetKurinByKeyQuery request, CancellationToken cancellationToken)
        {
            var kurin = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);

            if (kurin is null)
            {
                return new ServiceResult<KurinResponse>(ResultType.NotFound);
            }

            var kurinResponse = _mapper.Map<KurinResponse>(kurin);
            
            return new ServiceResult<KurinResponse>(ResultType.Success, kurinResponse);
        }
    }
}
