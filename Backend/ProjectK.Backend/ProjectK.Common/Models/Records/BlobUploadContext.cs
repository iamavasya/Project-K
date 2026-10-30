namespace ProjectK.Common.Models.Records;

public enum BlobUploadProcessingMode
{
    CompressToJpeg,
    EncodeAsPng,

    /// <summary>Kept byte for byte, but only after the bytes are checked to be a PDF.</summary>
    KeepPdf
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

    /// <summary>A picture inside a third-probe project. Resized like a photo: it is read on a phone.</summary>
    public static BlobUploadContext ProbeProjectImage { get; } =
        new(BlobUploadFolders.ProbeProjectImages, BlobUploadProcessingMode.CompressToJpeg, "image/jpeg");

    /// <summary>The посвідка of a third-prober's camp as a photo or scan.</summary>
    public static BlobUploadContext ThirdProbeCertificateImage { get; } =
        new(BlobUploadFolders.ThirdProbeCertificates, BlobUploadProcessingMode.CompressToJpeg, "image/jpeg");

    /// <summary>The same посвідка as a PDF.</summary>
    public static BlobUploadContext ThirdProbeCertificatePdf { get; } =
        new(BlobUploadFolders.ThirdProbeCertificates, BlobUploadProcessingMode.KeepPdf, "application/pdf");

    /// <summary>
    /// A screenshot attached to a problem report. Re-encoded like everything else, so nothing but
    /// a decoded image is stored; never resized, a screenshot is read for detail.
    /// </summary>
    public static BlobUploadContext FeedbackScreenshot { get; } =
        new(BlobUploadFolders.FeedbackScreenshots, BlobUploadProcessingMode.EncodeAsPng, "image/png");
}

public static class BlobUploadFolders
{
    public const string MemberPhotos = "member-photos";
    public const string GroupSilhouettes = "group-silhouettes";
    public const string ProbeProjectImages = "probe-project-images";
    public const string ThirdProbeCertificates = "third-probe-certificates";

    /// <summary>
    /// Not a scenario folder on purpose: nothing in the database references these blobs, and the
    /// orphan cleanup only sweeps <see cref="ScenarioFolders"/>, so an issue keeps its pictures.
    /// </summary>
    public const string FeedbackScreenshots = "feedback-screenshots";

    public static IReadOnlyCollection<string> ScenarioFolders { get; } =
    [
        MemberPhotos,
        GroupSilhouettes,
        ProbeProjectImages,
        ThirdProbeCertificates
    ];
}
