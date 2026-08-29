using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.KurinScope.Set
{
    public sealed class SetKurinScopeCommandValidator : AbstractValidator<SetKurinScopeCommand>
    {
        public SetKurinScopeCommandValidator()
        {
            // KurinKey is intentionally optional: null means "return to system-wide scope".
            RuleFor(command => command.UserKey).NotEmpty();
        }
    }
}
