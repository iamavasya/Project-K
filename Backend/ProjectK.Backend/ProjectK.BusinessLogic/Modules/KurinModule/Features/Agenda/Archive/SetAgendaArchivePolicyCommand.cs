using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Archive;

/// <summary>Sets how long a kurin's done tasks stay on the board and its archive is kept. Gated as kurin settings.</summary>
public sealed record SetAgendaArchivePolicyCommand(Guid KurinKey, int? AutoArchiveAfterDays, int? PurgeAfterDays)
    : IRequest<ServiceResult<AgendaArchivePolicyDto>>;

public sealed class SetAgendaArchivePolicyCommandHandler : IRequestHandler<SetAgendaArchivePolicyCommand, ServiceResult<AgendaArchivePolicyDto>>
{
    private readonly IUnitOfWork _uow;

    public SetAgendaArchivePolicyCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ServiceResult<AgendaArchivePolicyDto>> Handle(SetAgendaArchivePolicyCommand request, CancellationToken cancellationToken)
    {
        if (request.AutoArchiveAfterDays is < 1 or > 3650 || request.PurgeAfterDays is < 1 or > 3650)
        {
            return ServiceResult<AgendaArchivePolicyDto>.Failure(ResultType.BadRequest, "AGENDA_ARCHIVE_POLICY_RANGE", "A period is between 1 and 3650 days, or empty to switch it off.");
        }

        var kurin = await _uow.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);
        if (kurin is null)
        {
            return ServiceResult<AgendaArchivePolicyDto>.Failure(ResultType.NotFound, "KURIN_NOT_FOUND", "Kurin was not found.");
        }

        kurin.TaskAutoArchiveAfterDays = request.AutoArchiveAfterDays;
        kurin.TaskArchivePurgeAfterDays = request.PurgeAfterDays;
        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<AgendaArchivePolicyDto>(ResultType.Success,
            new AgendaArchivePolicyDto { AutoArchiveAfterDays = kurin.TaskAutoArchiveAfterDays, PurgeAfterDays = kurin.TaskArchivePurgeAfterDays });
    }
}
