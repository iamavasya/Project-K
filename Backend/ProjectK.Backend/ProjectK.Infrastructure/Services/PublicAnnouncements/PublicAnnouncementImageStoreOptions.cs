namespace ProjectK.Infrastructure.Services.PublicAnnouncements;

public sealed class PublicAnnouncementImageStoreOptions
{
    public string? Path { get; set; }

    public string GetResolvedPath()
    {
        return string.IsNullOrWhiteSpace(Path)
            ? System.IO.Path.Combine(AppContext.BaseDirectory, "App_Data", "public-announcement-images")
            : Path;
    }
}
