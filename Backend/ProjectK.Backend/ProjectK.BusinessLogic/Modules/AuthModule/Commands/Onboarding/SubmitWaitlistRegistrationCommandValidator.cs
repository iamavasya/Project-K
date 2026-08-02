using System.Linq;
using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    public sealed class SubmitWaitlistRegistrationCommandValidator : AbstractValidator<SubmitWaitlistRegistrationCommand>
    {
        public SubmitWaitlistRegistrationCommandValidator()
        {
            // Stop at the first failing rule per property so one message is returned at a time,
            // matching the handler's previous first-check-wins behaviour.
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
            RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(32);

            RuleFor(command => command.Stanytsia)
                .NotEmpty().WithMessage("Stanytsia is required.")
                .MaximumLength(120).WithMessage("Stanytsia must be 120 characters or fewer.");

            RuleFor(command => command.RegionOrCountry)
                .NotEmpty().WithMessage("Region is required.")
                .MaximumLength(120).WithMessage("Region must be 120 characters or fewer.");

            RuleFor(command => command.IsKurinLeaderCandidate)
                .Equal(true).WithMessage("Kurin leader confirmation is required.");

            RuleFor(command => command.ClaimedKurinNameOrNumber)
                .NotEmpty().WithMessage("Kurin number is required.")
                .Must(BeNumeric).WithMessage("Kurin number must contain only digits.");
        }

        private static bool BeNumeric(string? value)
        {
            var trimmed = value?.Trim();
            return !string.IsNullOrEmpty(trimmed)
                   && trimmed.All(char.IsDigit)
                   && int.TryParse(trimmed, out _);
        }
    }
}
