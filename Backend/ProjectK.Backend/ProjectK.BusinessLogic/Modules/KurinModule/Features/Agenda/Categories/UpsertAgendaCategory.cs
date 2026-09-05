using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Categories;

/// <summary>Create (empty key) or edit an event group. Authorization is enforced at the controller (Зв'язковий).</summary>
public sealed record UpsertAgendaCategory : IRequest<ServiceResult<AgendaCategoryResponse>>
{
    public Guid? AgendaCategoryKey { get; init; }
    public Guid KurinKey { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ColorHex { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int? Capacity { get; init; }
    public bool WaitlistEnabled { get; init; }
    public string? DefaultDescription { get; init; }
    public bool RsvpRequired { get; init; }
    public int? DefaultDurationMinutes { get; init; }
    public int? ReminderLeadMinutes { get; init; }
    public bool IsArchived { get; init; }
}

public sealed class UpsertAgendaCategoryHandler : IRequestHandler<UpsertAgendaCategory, ServiceResult<AgendaCategoryResponse>>
{
    private readonly IUnitOfWork _uow;

    public UpsertAgendaCategoryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ServiceResult<AgendaCategoryResponse>> Handle(UpsertAgendaCategory request, CancellationToken cancellationToken)
    {
        AgendaCategory category;
        var isNew = request.AgendaCategoryKey is null || request.AgendaCategoryKey == Guid.Empty;
        if (isNew)
        {
            category = new AgendaCategory { KurinKey = request.KurinKey };
            _uow.AgendaCategories.Create(category, cancellationToken);
        }
        else
        {
            var existing = await _uow.AgendaCategories.GetByKeyAsync(request.AgendaCategoryKey!.Value, cancellationToken);
            if (existing is null)
            {
                return ServiceResult<AgendaCategoryResponse>.Failure(ResultType.NotFound, "CATEGORY_NOT_FOUND", "Event group was not found.");
            }

            if (existing.KurinKey != request.KurinKey)
            {
                return ServiceResult<AgendaCategoryResponse>.Failure(ResultType.Forbidden, "CATEGORY_OTHER_KURIN", "Event group belongs to a different kurin.");
            }

            category = existing;
            _uow.AgendaCategories.Update(category, cancellationToken);
        }

        category.Name = request.Name.Trim();
        category.ColorHex = request.ColorHex.Trim();
        category.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        category.Capacity = request.Capacity;
        category.WaitlistEnabled = request.WaitlistEnabled;
        category.DefaultDescription = string.IsNullOrWhiteSpace(request.DefaultDescription) ? null : request.DefaultDescription.Trim();
        category.RsvpRequired = request.RsvpRequired;
        category.DefaultDurationMinutes = request.DefaultDurationMinutes;
        category.ReminderLeadMinutes = request.ReminderLeadMinutes;
        category.IsArchived = request.IsArchived;
        category.UpdatedDate = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<AgendaCategoryResponse>(
            isNew ? ResultType.Created : ResultType.Success,
            AgendaCategoryResponse.From(category));
    }
}
