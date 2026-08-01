using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    public sealed class SubmitWaitlistRegistrationCommandValidator : AbstractValidator<SubmitWaitlistRegistrationCommand>
    {
        public SubmitWaitlistRegistrationCommandValidator()
        {
            RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
            RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(32);
        }
    }
}
