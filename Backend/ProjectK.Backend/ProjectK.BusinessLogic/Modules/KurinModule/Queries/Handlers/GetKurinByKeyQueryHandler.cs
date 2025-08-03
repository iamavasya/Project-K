using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.Kurin.Queries.Handlers
{
    public class GetKurinByKeyQueryHandler : IRequestHandler<GetKurinByKeyQuery, KurinResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetKurinByKeyQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<KurinResponse> Handle(GetKurinByKeyQuery request, CancellationToken token)
        {
            var kurinResponse = new KurinResponse();
            var kurin = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, token);

            if (kurin != null)
            {
                kurinResponse = _mapper.Map<KurinResponse>(kurin);
            }

            return kurinResponse;
        }
    }
}
