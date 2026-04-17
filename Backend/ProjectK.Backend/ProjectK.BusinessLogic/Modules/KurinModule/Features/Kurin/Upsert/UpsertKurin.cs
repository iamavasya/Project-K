using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert
{
    public class UpsertKurin : IRequest<ServiceResult<KurinResponse>>
    {
        public Guid KurinKey { get; set; }
        public int Number { get; set; }
        public UpsertKurin(Guid kurinKey, int number)
        {
            KurinKey = kurinKey;
            Number = number;
        }
        public UpsertKurin(int number)
        {
            Number = number;
        }
    }

    public class UpsertKurinHandler : IRequestHandler<UpsertKurin, ServiceResult<KurinResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpsertKurinHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;

        }
        public async Task<ServiceResult<KurinResponse>> Handle(UpsertKurin request, CancellationToken cancellationToken)
        {
            var existing = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);

            bool isCreated = false;

            if (existing is null)
            {
                var isExistingByNumber = await _unitOfWork.Kurins.ExistsAsync(request.Number, cancellationToken);
                if (isExistingByNumber)
                {
                    var existingByNumber = await _unitOfWork.Kurins.GetByNumberAsync(request.Number, cancellationToken);
                    return new ServiceResult<KurinResponse>(ResultType.Conflict, _mapper.Map<KurinResponse>(existingByNumber));
                }
                // Create new Kurin
                existing = new(request.Number);
                _unitOfWork.Kurins.Create(existing, cancellationToken);
                isCreated = true;
            }
            else
            {
                // Update existing Kurin
                _mapper.Map(request, existing);
                _unitOfWork.Kurins.Update(existing, cancellationToken);
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<KurinResponse>(
                    ResultType.InternalServerError);
            }

            var kurinResponse = _mapper.Map<KurinResponse>(existing);

            return isCreated
                ? new ServiceResult<KurinResponse>(
                    ResultType.Created,
                    kurinResponse,
                    CreatedAtActionName: "GetByKey",
                    CreatedAtRouteValues: new { kurinKey = kurinResponse.KurinKey })
                : new ServiceResult<KurinResponse>(ResultType.Success, kurinResponse);
        }
    }
}
