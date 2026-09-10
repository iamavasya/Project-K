namespace ProjectK.Common.Models.Records;

/// <summary>
/// The code a person can read out or paste so another kurin can find them without searching by name.
/// <para>
/// It is derived from the member's own key rather than drawn at random, so the same person always
/// has the same code, the backfill for existing members is a plain SQL expression, and nothing has to
/// be stored twice. Hexadecimal keeps it unambiguous out loud — no O against 0, no I against 1.
/// </para>
/// </summary>
public static class MemberPublicId
{
    public const string Prefix = "PL";

    /// <summary>The code for a member key, e.g. <c>PL-4F7K2-9M4A1</c>.</summary>
    public static string For(Guid memberKey)
    {
        var digits = memberKey.ToString("N").ToUpperInvariant();
        return $"{Prefix}-{digits[..5]}-{digits[5..10]}";
    }
}
