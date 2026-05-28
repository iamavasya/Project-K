using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Services.Caching;
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
        public string? Stanytsia { get; set; }
        public string? RegionOrCountry { get; set; }
        public string? NamedAfter { get; set; }
        public string? Description { get; set; }

        public UpsertKurin(Guid kurinKey, int number, string? stanytsia = null, string? regionOrCountry = null, string? namedAfter = null, string? description = null)
        {
            KurinKey = kurinKey;
            Number = number;
            Stanytsia = stanytsia;
            RegionOrCountry = regionOrCountry;
            NamedAfter = namedAfter;
            Description = description;
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
        private readonly IBackendCache _cache;
        public UpsertKurinHandler(IUnitOfWork unitOfWork, IMapper mapper, IBackendCache cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;

        }
        public async Task<ServiceResult<KurinResponse>> Handle(UpsertKurin request, CancellationToken cancellationToken)
        {
            var stanytsia = NormalizeOptionalText(request.Stanytsia);
            var regionOrCountry = NormalizeOptionalText(request.RegionOrCountry);
            var namedAfter = NormalizeOptionalText(request.NamedAfter);
            var description = NormalizeOptionalText(request.Description);

            if (stanytsia?.Length > 120)
            {
                return ServiceResult<KurinResponse>.Failure(ResultType.BadRequest, "StanytsiaTooLong", "Stanytsia must be 120 characters or fewer.");
            }

            if (regionOrCountry?.Length > 120)
            {
                return ServiceResult<KurinResponse>.Failure(ResultType.BadRequest, "RegionOrCountryTooLong", "Region must be 120 characters or fewer.");
            }

            if (namedAfter?.Length > 200)
            {
                return ServiceResult<KurinResponse>.Failure(ResultType.BadRequest, "NamedAfterTooLong", "Named after must be 200 characters or fewer.");
            }

            if (description?.Length > 4000)
            {
                return ServiceResult<KurinResponse>.Failure(ResultType.BadRequest, "DescriptionTooLong", "Description must be 4000 characters or fewer.");
            }

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
                existing.Stanytsia = stanytsia;
                existing.RegionOrCountry = regionOrCountry;
                existing.NamedAfter = namedAfter;
                existing.Description = description;
                _unitOfWork.Kurins.Update(existing, cancellationToken);
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<KurinResponse>(
                    ResultType.InternalServerError);
            }

            var kurinResponse = _mapper.Map<KurinResponse>(existing);
            _cache.Invalidate(BackendCachePolicies.KurinReads);
            _cache.Invalidate(BackendCachePolicies.GroupReads);

            return isCreated
                ? new ServiceResult<KurinResponse>(
                    ResultType.Created,
                    kurinResponse,
                    CreatedAtActionName: "GetByKey",
                    CreatedAtRouteValues: new { kurinKey = kurinResponse.KurinKey })
                : new ServiceResult<KurinResponse>(ResultType.Success, kurinResponse);
        }

        private static string? NormalizeOptionalText(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }
    }
}
