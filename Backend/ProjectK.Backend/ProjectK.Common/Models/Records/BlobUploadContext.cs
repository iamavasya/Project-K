namespace ProjectK.Common.Models.Records
{
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

        public static BlobUploadContext PublicAnnouncement { get; } =
            new(BlobUploadFolders.PublicAnnouncements, BlobUploadProcessingMode.CompressToJpeg, "image/jpeg");
    }

    public static class BlobUploadFolders
    {
        public const string MemberPhotos = "member-photos";
        public const string GroupSilhouettes = "group-silhouettes";
        public const string PublicAnnouncements = "public-announcements";

        public static IReadOnlyCollection<string> ScenarioFolders { get; } =
        [
            MemberPhotos,
            GroupSilhouettes,
            PublicAnnouncements
        ];
    }
}
