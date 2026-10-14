using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using CatalogBadge = ProjectK.Common.Models.Catalog.Badge;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Catalog;

/// <summary>The вмілості a kurin's members may start, by its край's profile.</summary>
public sealed record GetKurinBadgesQuery(Guid KurinKey) : IRequest<ServiceResult<IReadOnlyList<CatalogBadge>>>;

/// <summary>The проби a kurin works with, by its край's profile.</summary>
public sealed record GetKurinProbesQuery(Guid KurinKey) : IRequest<ServiceResult<IReadOnlyList<ProbeSummaryResponse>>>;

public sealed class GetKurinBadgesQueryHandler : IRequestHandler<GetKurinBadgesQuery, ServiceResult<IReadOnlyList<CatalogBadge>>>
{
    private readonly IKurinCatalog _catalog;

    public GetKurinBadgesQueryHandler(IKurinCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<ServiceResult<IReadOnlyList<CatalogBadge>>> Handle(GetKurinBadgesQuery request, CancellationToken cancellationToken)
    {
        var catalog = await _catalog.ForAsync(request.KurinKey, cancellationToken);
        return catalog is null
            ? ServiceResult<IReadOnlyList<CatalogBadge>>.Failure(ResultType.NotFound, "KurinNotFound", "Kurin not found.")
            : new ServiceResult<IReadOnlyList<CatalogBadge>>(ResultType.Success, catalog.Badges);
    }
}

public sealed class GetKurinProbesQueryHandler : IRequestHandler<GetKurinProbesQuery, ServiceResult<IReadOnlyList<ProbeSummaryResponse>>>
{
    private readonly IKurinCatalog _catalog;

    public GetKurinProbesQueryHandler(IKurinCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<ServiceResult<IReadOnlyList<ProbeSummaryResponse>>> Handle(GetKurinProbesQuery request, CancellationToken cancellationToken)
    {
        var catalog = await _catalog.ForAsync(request.KurinKey, cancellationToken);
        return catalog is null
            ? ServiceResult<IReadOnlyList<ProbeSummaryResponse>>.Failure(ResultType.NotFound, "KurinNotFound", "Kurin not found.")
            : new ServiceResult<IReadOnlyList<ProbeSummaryResponse>>(ResultType.Success, catalog.Probes.Select(ProbeSummaryResponse.FromProbe).ToList());
    }
}
