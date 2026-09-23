using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Review;

/// <summary>
/// A review has to say which way it went. Left as a plain <c>bool</c> this defaulted to
/// <c>false</c>, so a body that never mentioned the verdict quietly refused the вмілість.
/// </summary>
public sealed class ReviewBadgeProgressCommandValidator : AbstractValidator<ReviewBadgeProgressCommand>
{
    public ReviewBadgeProgressCommandValidator()
    {
        RuleFor(command => command.MemberKey).NotEmpty();
        RuleFor(command => command.BadgeId).NotEmpty();
        RuleFor(command => command.IsApproved)
            .NotNull()
            .WithMessage("Say whether the вмілість is confirmed or refused.");

    }
}
