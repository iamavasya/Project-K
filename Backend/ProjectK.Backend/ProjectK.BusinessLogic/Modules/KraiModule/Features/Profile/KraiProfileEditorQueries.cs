using MediatR;
using ProjectK.BusinessLogic.Modules.KraiModule.Models;
using ProjectK.BusinessLogic.Modules.KraiModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KraiModule.Features.Profile;

/// <summary>The край's profile as the admin edits it.</summary>
public sealed record GetKraiProfileQuery(string KraiCode) : IRequest<ServiceResult<KraiProfileEditorResponse>>;

/// <summary>Every saved revision of the край's profile, newest first.</summary>
public sealed record GetKraiProfileRevisionsQuery(string KraiCode) : IRequest<ServiceResult<IReadOnlyList<KraiProfileRevisionResponse>>>;

public sealed class GetKraiProfileQueryHandler : IRequestHandler<GetKraiProfileQuery, ServiceResult<KraiProfileEditorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKraiProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<KraiProfileEditorResponse>> Handle(GetKraiProfileQuery request, CancellationToken cancellationToken)
    {
        var code = request.KraiCode.Trim().ToLowerInvariant();
        var krai = await _unitOfWork.Krais.GetAsync(code, cancellationToken);
        if (krai is null)
        {
            return ServiceResult<KraiProfileEditorResponse>.Failure(ResultType.NotFound, "KraiNotFound", "Krai not found.");
        }

        var revision = await _unitOfWork.Krais.GetLatestProfileRevisionAsync(code, cancellationToken);
        return new ServiceResult<KraiProfileEditorResponse>(ResultType.Success, new KraiProfileEditorResponse
        {
            KraiCode = krai.Code,
            KraiName = krai.Name,
            Revision = revision?.Revision ?? 0,
            // A revision that does not parse is shown empty: saving over it is how it gets fixed.
            Document = KraiProfileEditor.TryParse(revision?.Json)
        });
    }
}

public sealed class GetKraiProfileRevisionsQueryHandler : IRequestHandler<GetKraiProfileRevisionsQuery, ServiceResult<IReadOnlyList<KraiProfileRevisionResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKraiProfileRevisionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<IReadOnlyList<KraiProfileRevisionResponse>>> Handle(GetKraiProfileRevisionsQuery request, CancellationToken cancellationToken)
    {
        var code = request.KraiCode.Trim().ToLowerInvariant();
        if (!await _unitOfWork.Krais.ExistsAsync(code, cancellationToken))
        {
            return ServiceResult<IReadOnlyList<KraiProfileRevisionResponse>>.Failure(ResultType.NotFound, "KraiNotFound", "Krai not found.");
        }

        var revisions = await _unitOfWork.Krais.GetProfileRevisionsAsync(code, cancellationToken);
        var authorKeys = revisions.Select(r => r.CreatedByUserKey).OfType<Guid>().ToHashSet();
        var names = authorKeys.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _unitOfWork.Users.GetAllAsync(cancellationToken))
                .Where(u => authorKeys.Contains(u.Id))
                .ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim() is { Length: > 0 } name ? name : u.Email ?? string.Empty);

        IReadOnlyList<KraiProfileRevisionResponse> response = revisions
            .Select(r => new KraiProfileRevisionResponse
            {
                Revision = r.Revision,
                CreatedAtUtc = r.CreatedAtUtc,
                CreatedByName = r.CreatedByUserKey is { } key ? names.GetValueOrDefault(key) : null,
                Note = r.Note
            })
            .ToList();

        return new ServiceResult<IReadOnlyList<KraiProfileRevisionResponse>>(ResultType.Success, response);
    }
}
