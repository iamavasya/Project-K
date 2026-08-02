using FluentValidation;
using ProjectK.Common.Models.Dtos.UserModule;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public sealed class ResetTileLayoutCommandValidator : AbstractValidator<ResetTileLayoutCommand>
    {
        public ResetTileLayoutCommandValidator()
        {
            RuleFor(command => command.BoardKey)
                .Must(TileBoardKeys.All.Contains)
                .WithMessage("Unknown board key.");
        }
    }
}
