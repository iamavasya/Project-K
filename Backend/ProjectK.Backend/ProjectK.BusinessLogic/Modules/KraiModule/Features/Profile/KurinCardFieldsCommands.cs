using MediatR;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KraiModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.KraiProfiles;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KraiModule.Features.Profile;

/// <summary>One field a kurin may leave off its card, and whether it does.</summary>
public sealed record KurinCardFieldResponse(string Key, string Label, bool Hidden);

/// <summary>The card fields a kurin may hide, for its settings page.</summary>
public sealed record GetKurinCardFieldsQuery(Guid KurinKey) : IRequest<ServiceResult<IReadOnlyList<KurinCardFieldResponse>>>;

/// <summary>Which of them the kurin hides; a key it may not hide is dropped.</summary>
public sealed record SetKurinCardFieldsCommand(Guid KurinKey, IReadOnlyCollection<string> Hidden) : IRequest<ServiceResult<IReadOnlyList<KurinCardFieldResponse>>>;

public sealed class GetKurinCardFieldsQueryHandler : IRequestHandler<GetKurinCardFieldsQuery, ServiceResult<IReadOnlyList<KurinCardFieldResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKraiProfileProvider _profiles;

    public GetKurinCardFieldsQueryHandler(IUnitOfWork unitOfWork, IKraiProfileProvider profiles)
    {
        _unitOfWork = unitOfWork;
        _profiles = profiles;
    }

    public async Task<ServiceResult<IReadOnlyList<KurinCardFieldResponse>>> Handle(GetKurinCardFieldsQuery request, CancellationToken cancellationToken)
    {
        var kurin = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);
        var profile = kurin is null ? null : await _profiles.GetForKraiAsync(kurin.KraiCode, cancellationToken);
        return kurin is null || profile is null
            ? KurinCardFieldsResult.NotFound()
            : KurinCardFieldsResult.Of(profile.CardFields, kurin.HiddenCardFields);
    }
}

public sealed class SetKurinCardFieldsCommandHandler : IRequestHandler<SetKurinCardFieldsCommand, ServiceResult<IReadOnlyList<KurinCardFieldResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKraiProfileProvider _profiles;
    private readonly IBackendCache _cache;

    public SetKurinCardFieldsCommandHandler(IUnitOfWork unitOfWork, IKraiProfileProvider profiles, IBackendCache cache)
    {
        _unitOfWork = unitOfWork;
        _profiles = profiles;
        _cache = cache;
    }

    public async Task<ServiceResult<IReadOnlyList<KurinCardFieldResponse>>> Handle(SetKurinCardFieldsCommand request, CancellationToken cancellationToken)
    {
        var kurin = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);
        var profile = kurin is null ? null : await _profiles.GetForKraiAsync(kurin.KraiCode, cancellationToken);
        if (kurin is null || profile is null)
        {
            return KurinCardFieldsResult.NotFound();
        }

        var hideable = profile.CardFields.Hideable().Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        kurin.HiddenCardFields = KurinHiddenCardFields.Write(request.Hidden.Where(hideable.Contains));
        _unitOfWork.Kurins.Update(kurin, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cache.Invalidate(BackendCachePolicies.KurinReads);

        return KurinCardFieldsResult.Of(profile.CardFields, kurin.HiddenCardFields);
    }
}

internal static class KurinCardFieldsResult
{
    public static ServiceResult<IReadOnlyList<KurinCardFieldResponse>> Of(KraiCardFieldsSettings card, string? hiddenJson)
    {
        var hidden = KurinHiddenCardFields.Read(hiddenJson);
        return new ServiceResult<IReadOnlyList<KurinCardFieldResponse>>(
            ResultType.Success,
            card.Hideable().Select(field => new KurinCardFieldResponse(field.Key, field.Label, hidden.Contains(field.Key))).ToList());
    }

    public static ServiceResult<IReadOnlyList<KurinCardFieldResponse>> NotFound()
    {
        return ServiceResult<IReadOnlyList<KurinCardFieldResponse>>.Failure(ResultType.NotFound, "KurinNotFound", "Kurin not found.");
    }
}
