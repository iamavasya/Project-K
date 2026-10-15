using System.Globalization;
using ProjectK.Common.Models.KraiProfiles;
using MemberEntity = ProjectK.Common.Entities.KurinModule.Member;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;

/// <summary>
/// What a край's card fields do to a write from the form: a hidden field keeps what it had, a
/// required one must be filled, and the край's own fields are merged into what the member has.
/// </summary>
public static class MemberCardFields
{
    public const string ErrorCode = "CardFieldRequired";

    private const int MaxValueLength = 200;

    /// <summary>The member's values of the край's own fields once this write lands.</summary>
    public sealed record Outcome(string? CardExtras, string? Error);

    public static Outcome Apply(KraiCardFieldsSettings card, UpsertMemberProfileCommand request, MemberEntity? existing)
    {
        if (card.ModeOf(KraiCardFields.MiddleName) == KraiCardFields.Hidden)
        {
            request.MiddleName = existing?.MiddleName ?? string.Empty;
        }

        if (card.ModeOf(KraiCardFields.PhoneNumber) == KraiCardFields.Hidden)
        {
            request.PhoneNumber = existing?.PhoneNumber ?? string.Empty;
        }

        var missing = new[]
            {
                (Field: KraiCardFields.MiddleName, Value: request.MiddleName),
                (Field: KraiCardFields.PhoneNumber, Value: request.PhoneNumber)
            }
            .FirstOrDefault(field => card.ModeOf(field.Field) == KraiCardFields.Required && string.IsNullOrWhiteSpace(field.Value));

        if (missing.Field is not null)
        {
            return new Outcome(existing?.CardExtras, $"Поле «{KraiCardFields.Labels[missing.Field]}» обовʼязкове.");
        }

        var values = MemberCardExtras.Read(existing?.CardExtras);
        foreach (var field in card.Extra)
        {
            if (request.CardExtras?.TryGetValue(field.Key, out var sent) == true)
            {
                var value = sent?.Trim() ?? string.Empty;
                if (value.Length > MaxValueLength)
                {
                    return new Outcome(existing?.CardExtras, $"Поле «{field.Label}» задовге.");
                }

                if (value.Length > 0 && field.Type == KraiCardFields.Date
                    && !DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return new Outcome(existing?.CardExtras, $"Поле «{field.Label}» має бути датою.");
                }

                if (value.Length == 0)
                {
                    values.Remove(field.Key);
                }
                else
                {
                    values[field.Key] = value;
                }
            }

            if (field.Required && !values.ContainsKey(field.Key))
            {
                return new Outcome(existing?.CardExtras, $"Поле «{field.Label}» обовʼязкове.");
            }
        }

        return new Outcome(MemberCardExtras.Write(values), null);
    }
}
