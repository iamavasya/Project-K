using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Dtos;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpsertKurinCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;

        }
        public async Task<KurinResponse> Handle(UpsertKurinCommand request, CancellationToken token)
        {
            var existing = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, token);

            if (existing is null)
            {
                // Create new Kurin
                existing = new (request.Number);
                _unitOfWork.Kurins.Create(existing, token);
            }
            else
            {
                // Update existing Kurin
                _mapper.Map(request, existing);
                _unitOfWork.Kurins.Update(existing, token);
            }
            var result = await _unitOfWork.SaveChangesAsync(token);
            if (result <= 0)
            {
                throw new InvalidOperationException("Failed to save changes to the database.");
            }
            return _mapper.Map(existing, new KurinResponse());

        }
    }
}
