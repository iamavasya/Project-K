using MediatR;
using ProjectK.BusinessLogic.Modules.KraiModule.Models;
using ProjectK.BusinessLogic.Modules.KraiModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.KraiProfiles;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KraiModule.Features.Profile;

/// <summary>Saves the край's profile as a new revision. Also how an imported file is saved.</summary>
public sealed record SaveKraiProfileCommand(string KraiCode, int BaseRevision, KraiProfileDocument Document, string? Note)
    : IRequest<ServiceResult<KraiProfileEditorResponse>>;

/// <summary>Brings an earlier revision back by saving its document again as the newest one.</summary>
public sealed record RestoreKraiProfileRevisionCommand(string KraiCode, int Revision)
    : IRequest<ServiceResult<KraiProfileEditorResponse>>;

public sealed class SaveKraiProfileCommandHandler : IRequestHandler<SaveKraiProfileCommand, ServiceResult<KraiProfileEditorResponse>>
{
    private readonly KraiProfileEditor _editor;

    public SaveKraiProfileCommandHandler(KraiProfileEditor editor)
    {
        _editor = editor;
    }

    public Task<ServiceResult<KraiProfileEditorResponse>> Handle(SaveKraiProfileCommand request, CancellationToken cancellationToken)
    {
        return _editor.SaveAsync(request.KraiCode, request.BaseRevision, request.Document, request.Note, cancellationToken);
    }
}

public sealed class RestoreKraiProfileRevisionCommandHandler : IRequestHandler<RestoreKraiProfileRevisionCommand, ServiceResult<KraiProfileEditorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly KraiProfileEditor _editor;

    public RestoreKraiProfileRevisionCommandHandler(IUnitOfWork unitOfWork, KraiProfileEditor editor)
    {
        _unitOfWork = unitOfWork;
        _editor = editor;
    }

    public async Task<ServiceResult<KraiProfileEditorResponse>> Handle(RestoreKraiProfileRevisionCommand request, CancellationToken cancellationToken)
    {
        var code = request.KraiCode.Trim().ToLowerInvariant();
        var source = await _unitOfWork.Krais.GetProfileRevisionAsync(code, request.Revision, cancellationToken);
        if (source is null)
        {
            return ServiceResult<KraiProfileEditorResponse>.Failure(ResultType.NotFound, "RevisionNotFound", "Revision not found.");
        }

        var latest = await _unitOfWork.Krais.GetLatestProfileRevisionAsync(code, cancellationToken);
        return await _editor.SaveAsync(
            code,
            latest?.Revision ?? 0,
            KraiProfileEditor.TryParse(source.Json),
            $"Відновлено ревізію {source.Revision}",
            cancellationToken);
    }
}
