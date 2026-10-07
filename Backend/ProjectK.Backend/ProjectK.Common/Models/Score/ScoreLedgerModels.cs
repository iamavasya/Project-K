using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Score;

/// <summary>Days to total, both ends inclusive: a пластовий рік or a stage.</summary>
public readonly record struct ScorePeriod(DateOnly From, DateOnly To)
{
    public bool Contains(DateOnly day) => day >= From && day <= To;
}

/// <summary>
/// A youth membership as the score sees it. Only youths are scored; a впорядник is never passed in.
/// <see cref="GroupKey"/> is where they stand now — where they stood before comes from the moves.
/// </summary>
public sealed record ScoreMember(Guid MembershipKey, Guid? GroupKey, DateOnly JoinedOn, DateOnly? LeftOn);

/// <summary>
/// A fact another module knows that is worth points — a вмілість confirmed, a quarter paid. The
/// score module asks for these; it does not read the tables they live in.
/// </summary>
/// <param name="Variant">The level for a пересторога; 0 otherwise.</param>
public sealed record ScoreFact(Guid MembershipKey, ScoreSource Source, int Variant, DateOnly On);

/// <summary>Points earned once: who, for which гурток (if any), from where, when.</summary>
public sealed record ScoreLine(Guid MembershipKey, Guid? GroupKey, ScoreSource Source, int Points, DateOnly On);

/// <summary>One person's points in a period — in one гурток, or in all of them.</summary>
public sealed record ScorePersonTotal(Guid MembershipKey, int Total, IReadOnlyDictionary<ScoreSource, int> BySource);

/// <summary>One гурток's standing in a period.</summary>
/// <param name="YouthPoints">Everything its youths earned while in it.</param>
/// <param name="YouthCount">How many youths it had, a youth there for half the period counting a half.</param>
/// <param name="Average">The youths' points per youth.</param>
/// <param name="GroupPoints">What was given to the гурток as a whole; never divided.</param>
/// <param name="Score">What it is ranked by: the average or the sum, as the kurin chose, plus <paramref name="GroupPoints"/>.</param>
/// <param name="OtherScore">The same by the other method, shown alongside.</param>
public sealed record ScoreGroupTotal(
    Guid GroupKey,
    int YouthPoints,
    decimal YouthCount,
    decimal Average,
    int GroupPoints,
    decimal Score,
    decimal OtherScore);
