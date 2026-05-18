namespace ProjectK.Common.Models.Records;

public sealed record PublicAnnouncementImageFile(
    Stream Content,
    string FileName,
    string ContentType);
