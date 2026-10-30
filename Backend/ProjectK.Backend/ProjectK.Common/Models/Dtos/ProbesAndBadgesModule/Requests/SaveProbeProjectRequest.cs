namespace ProjectK.Common.Models.Dtos.ProbesAndBadgesModule.Requests;

public class SaveProbeProjectRequest
{
    public string? Title { get; set; }
    public bool IsIndividual { get; set; }

    /// <summary>Markdown; pictures as <c>![…](project-image:{key})</c>.</summary>
    public string? Description { get; set; }

    public string? DirectionMentorName { get; set; }
    public string? DirectionMentorContact { get; set; }
}
