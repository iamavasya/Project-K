using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ActivateAccount
{
    public sealed class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
    {
        public ActivateAccountCommandValidator()
        {
            // Presence only; password complexity stays with the Identity password policy.
            RuleFor(command => command.Token).NotEmpty();
            RuleFor(command => command.Password).NotEmpty();
        }
    }
}
