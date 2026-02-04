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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Get
{
    public class GetKurinByKey : IRequest<ServiceResult<KurinResponse>>
    {
        public Guid KurinKey { get; set; }

        public GetKurinByKey(Guid kurinKey)
        {
            KurinKey = kurinKey;
        }
    }

    public class GetKurinByKeyHandler : IRequestHandler<GetKurinByKey, ServiceResult<KurinResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetKurinByKeyHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<KurinResponse>> Handle(GetKurinByKey request, CancellationToken cancellationToken)
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
