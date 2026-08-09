using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Update;

public sealed class UpdateAgendaItemValidator : AbstractValidator<UpdateAgendaItem>
{
    public UpdateAgendaItemValidator()
    {
        RuleFor(command => command.AgendaItemKey).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(2000);
        RuleFor(command => command.Targets).NotEmpty().WithMessage("At least one assignment target is required.");
        RuleForEach(command => command.Targets).ChildRules(target =>
            target.RuleFor(t => t.TargetKey).NotEmpty());

        RuleFor(command => command)
            .Must(c => !(c.EndUtc.HasValue && c.StartUtc is null))
            .WithMessage("EndUtc requires StartUtc.");
        RuleFor(command => command)
            .Must(c => !(c.StartUtc.HasValue && c.EndUtc.HasValue) || c.EndUtc!.Value >= c.StartUtc!.Value)
            .WithMessage("EndUtc must not be before StartUtc.");
    }
}
