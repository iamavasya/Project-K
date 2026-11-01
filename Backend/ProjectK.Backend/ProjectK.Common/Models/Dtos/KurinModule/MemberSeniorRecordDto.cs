namespace ProjectK.Common.Models.Dtos.KurinModule;

/// <summary>One line of a senior's книжечка. The client names it from the catalogue by <see cref="CatalogId"/>.</summary>
public class MemberSeniorRecordDto
{
    public Guid MemberSeniorRecordKey { get; set; }
    public string CatalogId { get; set; } = string.Empty;
    public string? Number { get; set; }
    public DateOnly? AchievedOn { get; set; }
    public string? Note { get; set; }
}
