using MediatR;
using ProjectK.BusinessLogic.Modules.KraiModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KraiModule.Features.List;

/// <summary>The краї this instance knows, with how many kurins each holds.</summary>
public sealed record GetKraisQuery : IRequest<ServiceResult<IReadOnlyList<KraiResponse>>>;

public sealed class GetKraisQueryHandler : IRequestHandler<GetKraisQuery, ServiceResult<IReadOnlyList<KraiResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKraisQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<IReadOnlyList<KraiResponse>>> Handle(GetKraisQuery request, CancellationToken cancellationToken)
    {
        var krais = await _unitOfWork.Krais.GetAllAsync(cancellationToken);
        var kurins = await _unitOfWork.Kurins.GetAllAsync(cancellationToken);
        var counts = kurins.GroupBy(k => k.KraiCode).ToDictionary(g => g.Key, g => g.Count());

        IReadOnlyList<KraiResponse> response = krais
            .Select(krai => new KraiResponse
            {
                Code = krai.Code,
                Name = krai.Name,
                KurinCount = counts.GetValueOrDefault(krai.Code)
            })
            .ToList();

        return new ServiceResult<IReadOnlyList<KraiResponse>>(ResultType.Success, response);
    }
}
