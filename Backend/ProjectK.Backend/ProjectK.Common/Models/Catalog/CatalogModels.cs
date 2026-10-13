namespace ProjectK.Common.Models.Catalog;

/// <summary>
/// A вмілість as Lilejka has always shown it. The catalogue service publishes richer data; this shape
/// stays so the clients keep working unchanged.
/// </summary>
public sealed record Badge(
    string Id,
    string Title,
    string ImagePath,
    string Country,
    string Specialization,
    string Status,
    int? Level,
    string LastUpdated,
    string SeekerRequirements,
    string InstructorRequirements,
    IReadOnlyList<string> FixNotes);

public sealed record BadgesMetadata(
    string ParserVersion,
    string ToolAuthor,
    string ParserComment,
    DateTimeOffset? ParsedAtUtc,
    string SourceUrl,
    bool FixerEnabled,
    string FixerMode,
    int TotalBadges);

/// <summary>
/// A probe. <see cref="Points"/> are its own points or, when it has none, every point of its
/// sections in order: progress is recorded against those ids.
/// </summary>
public sealed record Probe(
    string Id,
    string Title,
    IReadOnlyList<ProbePoint> Points,
    IReadOnlyList<ProbeSection>? Sections = null);

public sealed record ProbePoint(string Id, string Title);

public sealed record ProbeSection(string Id, string Code, string Title, IReadOnlyList<ProbePoint> Points);

/// <summary>An image downloaded from the catalogue.</summary>
public sealed record CatalogImage(byte[] Content, string ContentType);
