using FluentValidation;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.BusinessLogic.Modules.MeModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.MeModule.Features.Events;

/// <summary>
/// The person's answer to an event of any of their kurins, from the dashboard. Refused on an event
/// they could not see from that kurin's calendar — the one visibility rule for both.
/// </summary>
public sealed record SetMyEventResponseCommand(Guid AgendaItemKey, AgendaRsvpStatus Status) : IRequest<ServiceResult<object>>;

public sealed class SetMyEventResponseCommandValidator : AbstractValidator<SetMyEventResponseCommand>
{
    public SetMyEventResponseCommandValidator()
    {
        RuleFor(c => c.AgendaItemKey).NotEmpty();
        RuleFor(c => c.Status).IsInEnum();
    }
}

public sealed class SetMyEventResponseCommandHandler : IRequestHandler<SetMyEventResponseCommand, ServiceResult<object>>
{
    private readonly MeAgendaScopes _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public SetMyEventResponseCommandHandler(MeAgendaScopes scopes, IUnitOfWork unitOfWork, TimeProvider time)
    {
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<ServiceResult<object>> Handle(SetMyEventResponseCommand request, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.AgendaItems.GetByKeyWithAssignmentsAsync(request.AgendaItemKey, cancellationToken);
        if (item is null || item.Kind != AgendaItemKind.Event)
        {
            return ServiceResult<object>.Failure(ResultType.NotFound, "AGENDA_NOT_FOUND", "There is no such event.");
        }

        var context = (await _scopes.BuildAsync(cancellationToken)).FirstOrDefault(c => c.Membership.KurinKey == item.KurinKey);
        if (context is null || !AgendaPermissions.IsVisibleTo(item, context.Viewer))
        {
            return ServiceResult<object>.Failure(ResultType.Forbidden, "AGENDA_NOT_VISIBLE", "You cannot respond to this event.");
        }

        await AgendaRsvpWriter.UpsertAsync(_unitOfWork, item, context.Viewer.ViewerUserKey!.Value, request.Status, _time.GetUtcNow().UtcDateTime, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ServiceResult<object>(ResultType.Success);
    }
}
