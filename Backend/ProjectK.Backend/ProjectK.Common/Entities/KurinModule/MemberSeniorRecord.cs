namespace ProjectK.Common.Entities.KurinModule;

/// <summary>
/// One line of a senior's книжечка: a ступінь, відзначення or вишкіл from the catalogue, entered by
/// the person themself and confirmed by nobody. It belongs to the person, not to a kurin, and is
/// shown wherever their card is.
/// </summary>
public class MemberSeniorRecord : Entity
{
    public const int NumberMaxLength = 50;
    public const int NoteMaxLength = 500;

    public Guid MemberSeniorRecordKey { get; set; } = Guid.NewGuid();
    public Guid MemberKey { get; set; }
    public Member Member { get; set; } = null!;

    /// <summary>The catalogue id of the ступінь, відзначення or вишкіл.</summary>
    public string CatalogId { get; set; } = string.Empty;

    /// <summary>The number a вишкіл gives, «ЛШ 47», as the person writes it.</summary>
    public string? Number { get; set; }

    public DateOnly? AchievedOn { get; set; }
    public string? Note { get; set; }
}
