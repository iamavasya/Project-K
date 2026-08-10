using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Create;

public sealed class CreateAgendaItemValidator : AbstractValidator<CreateAgendaItem>
{
    public CreateAgendaItemValidator()
    {
        RuleFor(command => command.KurinKey).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(2000);
        RuleFor(command => command.Targets).NotEmpty().WithMessage("At least one assignment target is required.");
        RuleForEach(command => command.Targets).ChildRules(target =>
            target.RuleFor(t => t.TargetKey).NotEmpty());

        // An end without a start is meaningless, and an end must not precede a start.
        RuleFor(command => command)
            .Must(c => !(c.EndUtc.HasValue && c.StartUtc is null))
            .WithMessage("EndUtc requires StartUtc.");
        RuleFor(command => command)
            .Must(c => !(c.StartUtc.HasValue && c.EndUtc.HasValue) || c.EndUtc!.Value >= c.StartUtc!.Value)
            .WithMessage("EndUtc must not be before StartUtc.");
    }
}
