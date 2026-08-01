using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    /// <summary>
    /// Example validator wired through <c>ValidationBehavior</c>: a malformed or empty email is
    /// rejected with BadRequest before the handler runs. Well-formed unknown emails still pass
    /// (the handler keeps its anti-enumeration behaviour).
    /// </summary>
    public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
