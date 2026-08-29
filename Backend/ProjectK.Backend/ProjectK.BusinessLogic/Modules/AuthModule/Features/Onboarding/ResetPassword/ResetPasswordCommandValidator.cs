using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ResetPassword
{
    public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(command => command.Email).NotEmpty().EmailAddress();
            RuleFor(command => command.Token).NotEmpty();
            // Presence only; password complexity stays with the Identity password policy.
            RuleFor(command => command.NewPassword).NotEmpty();
        }
    }
}
