namespace ProjectK.Common.Models.Records;

public enum BlobUploadProcessingMode
{
    CompressToJpeg,
    EncodeAsPng
}

public sealed record BlobUploadContext(
    string Folder,
    BlobUploadProcessingMode ProcessingMode,
    string ContentType)
{
    public static BlobUploadContext MemberPhoto { get; } =
        new(BlobUploadFolders.MemberPhotos, BlobUploadProcessingMode.CompressToJpeg, "image/jpeg");

    public static BlobUploadContext GroupSilhouette { get; } =
        new(BlobUploadFolders.GroupSilhouettes, BlobUploadProcessingMode.EncodeAsPng, "image/png");

    /// <summary>
    /// A screenshot attached to a problem report. Re-encoded like everything else, so nothing but
    /// a decoded image reaches the public container; never resized, a screenshot is read for detail.
    /// </summary>
    public static BlobUploadContext FeedbackScreenshot { get; } =
        new(BlobUploadFolders.FeedbackScreenshots, BlobUploadProcessingMode.EncodeAsPng, "image/png");
}

public static class BlobUploadFolders
{
    public const string MemberPhotos = "member-photos";
    public const string GroupSilhouettes = "group-silhouettes";

    /// <summary>
    /// Not a scenario folder on purpose: nothing in the database references these blobs, and the
    /// orphan cleanup only sweeps <see cref="ScenarioFolders"/>, so an issue keeps its pictures.
    /// </summary>
    public const string FeedbackScreenshots = "feedback-screenshots";

    public static IReadOnlyCollection<string> ScenarioFolders { get; } =
    [
        MemberPhotos,
        GroupSilhouettes
    ];
}
