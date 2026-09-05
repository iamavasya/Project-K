using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Categories;

public sealed class UpsertAgendaCategoryValidator : AbstractValidator<UpsertAgendaCategory>
{
    public UpsertAgendaCategoryValidator()
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ColorHex).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Icon).MaximumLength(64);
        RuleFor(c => c.DefaultDescription).MaximumLength(2000);
        RuleFor(c => c.Capacity).GreaterThan(0).When(c => c.Capacity.HasValue);
        RuleFor(c => c.DefaultDurationMinutes).GreaterThan(0).When(c => c.DefaultDurationMinutes.HasValue);
        RuleFor(c => c.ReminderLeadMinutes).GreaterThanOrEqualTo(0).When(c => c.ReminderLeadMinutes.HasValue);
    }
}
