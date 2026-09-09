using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ResendInvitation;

public sealed class ResendInvitationByEmailCommandValidator : AbstractValidator<ResendInvitationByEmailCommand>
{
    public ResendInvitationByEmailCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
