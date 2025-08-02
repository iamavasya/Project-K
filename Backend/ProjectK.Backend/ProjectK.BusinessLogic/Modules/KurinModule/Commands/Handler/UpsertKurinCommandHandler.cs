using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Dtos;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Handler
{
    public class UpsertKurinCommandHandler : IRequestHandler<UpsertKurinCommand, KurinResponse>
    {
        private readonly IKurinRepository _kurinRepository;
        private readonly IMapper _mapper;
        public UpsertKurinCommandHandler(IKurinRepository kurinRepository, IMapper mapper)
        {
            _kurinRepository = kurinRepository;
            _mapper = mapper;

        }
        public async Task<KurinResponse> Handle(UpsertKurinCommand request, CancellationToken token)
        {
            var kurinDto = _mapper.Map<KurinDto>(request);
            var response = await _kurinRepository.GetByKeyOrCreateAsync(kurinDto, token);
            return _mapper.Map<KurinResponse>(response);
            
        }
    }
}
