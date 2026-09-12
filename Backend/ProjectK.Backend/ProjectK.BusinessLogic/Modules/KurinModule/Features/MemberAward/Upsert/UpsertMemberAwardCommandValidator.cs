using FluentValidation;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberAward.Upsert;

/// <summary>
/// A відзначення is a date and a level, and the command carried both as non-nullable value types
/// with nothing checking them. A request that simply did not mention the date answered 200 and
/// stored <see cref="DateTime.MinValue"/> — the card then read «0001». It was found by sending
/// the field under the wrong name, which is exactly how a client would meet it.
/// </summary>
public sealed class UpsertMemberAwardCommandValidator : AbstractValidator<UpsertMemberAwardCommand>
{
    /// <summary>
    /// A little slack so a провід in a zone ahead of UTC can record today's відзначення today.
    /// Without it «сьогодні» in Kyiv is the future in UTC for three hours every evening.
    /// </summary>
    private static readonly TimeSpan Tolerance = TimeSpan.FromDays(1);

    public UpsertMemberAwardCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.MemberKey).NotEmpty();

        // The enum starts at 1 precisely so that "not said" is not a valid level — see
        // MemberAwardLevel. This is what makes that intent hold at the boundary.
        RuleFor(command => command.Level)
            .IsInEnum()
            .WithMessage("A відзначення level is required.");

        RuleFor(command => command.DateAcquired)
            .NotEmpty()
            .WithMessage("The date the відзначення was earned is required.");

        RuleFor(command => command.DateAcquired)
            .Must(date => date <= timeProvider.GetUtcNow().UtcDateTime.Add(Tolerance))
            .WithMessage("A відзначення cannot be earned in the future.");

        RuleFor(command => command.Note).MaximumLength(2000);
    }
}
