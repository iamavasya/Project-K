using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward.Review;

/// <summary>
/// A review has to say which way it went. Left as a plain <c>bool</c> this defaulted to
/// <c>false</c>, so a body that never mentioned the verdict quietly refused the відзначення.
/// </summary>
public sealed class ReviewMemberAwardCommandValidator : AbstractValidator<ReviewMemberAwardCommand>
{
    public ReviewMemberAwardCommandValidator()
    {
        RuleFor(command => command.MemberAwardKey).NotEmpty();
        RuleFor(command => command.IsApproved)
            .NotNull()
            .WithMessage("Say whether the відзначення is confirmed or refused.");
    }
}
