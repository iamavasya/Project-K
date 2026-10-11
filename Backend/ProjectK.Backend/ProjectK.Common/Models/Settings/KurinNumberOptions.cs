namespace ProjectK.Common.Models.Settings;

/// <summary>
/// Which numbers a kurin may carry. The floor is fixed at one, so zero and below stay free for
/// kurins the team keeps for itself (a test kurin on prod); the ceiling is configuration, so it can
/// grow with the станиця instead of letting anyone open kurin 1153.
/// </summary>
public sealed class KurinNumberOptions
{
    public const string SectionName = "KurinNumbers";

    public const int MinNumber = 1;

    public int MaxNumber { get; init; } = 200;

    public bool IsAllowed(int number) => number >= MinNumber && number <= MaxNumber;
}

/// <summary>The inclusive range of kurin numbers, as the forms read it.</summary>
public sealed record KurinNumberRange(int Min, int Max);
