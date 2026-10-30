namespace ProjectK.Common.Models.Records;

/// <summary>
/// The limits of the third probe's projects. Numbers, not styling: inline pictures must not turn
/// blob storage into a dump, so the server holds them and the clients only repeat them.
/// </summary>
public static class ProbeProjectRules
{
    /// <summary>The programme asks for five projects; one more would be a plan, not a probe.</summary>
    public const int MaxProjects = 5;

    public const int MaxImagesPerProject = 5;

    /// <summary>One picture as it arrives, before it is re-encoded: 1 MB.</summary>
    public const long MaxImageBytes = 1024 * 1024;

    /// <summary>The description in Markdown, pictures as short tokens.</summary>
    public const int MaxDescriptionLength = 20_000;

    public const int MaxTitleLength = 200;
    public const int MaxMentorNameLength = 200;
    public const int MaxMentorContactLength = 200;

    /// <summary>The probe's place in its edition (знак прихильника is 0).</summary>
    public const int ProbeOrder = 3;

    /// <summary>How a picture is written into the Markdown: <c>![…](project-image:{key})</c>.</summary>
    public const string ImageScheme = "project-image:";
}
