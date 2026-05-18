using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Records;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ProjectK.Infrastructure.Services.PublicAnnouncements;

public sealed class LocalPublicAnnouncementImageStore : IPublicAnnouncementImageStore
{
    private const string ContentType = "image/jpeg";
    private const string Extension = ".jpg";
    private const int MaxDimension = 1920;

    private readonly string _rootPath;
    private readonly ILogger<LocalPublicAnnouncementImageStore> _logger;

    public LocalPublicAnnouncementImageStore(
        IOptions<PublicAnnouncementImageStoreOptions> options,
        ILogger<LocalPublicAnnouncementImageStore> logger)
    {
        _rootPath = options.Value.GetResolvedPath();
        _logger = logger;
    }

    public async Task<PublicAnnouncementImageUploadResult> SaveAsync(
        byte[] imageBytes,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        if (imageBytes.Length == 0)
        {
            throw new ArgumentException("Image content is empty.", nameof(imageBytes));
        }

        Directory.CreateDirectory(_rootPath);

        var key = $"{Guid.NewGuid():N}{Extension}";
        var path = GetPath(key);
        var processedBytes = await NormalizeToJpegAsync(imageBytes, fileName, cancellationToken);

        await File.WriteAllBytesAsync(path, processedBytes, cancellationToken);

        return new PublicAnnouncementImageUploadResult(key, key, ContentType);
    }

    public Task<PublicAnnouncementImageFile?> OpenAsync(string imageKey, CancellationToken cancellationToken)
    {
        if (!TryNormalizeKey(imageKey, out var normalizedKey))
        {
            return Task.FromResult<PublicAnnouncementImageFile?>(null);
        }

        var path = GetPath(normalizedKey);
        if (!File.Exists(path))
        {
            return Task.FromResult<PublicAnnouncementImageFile?>(null);
        }

        Stream stream = File.OpenRead(path);
        return Task.FromResult<PublicAnnouncementImageFile?>(new PublicAnnouncementImageFile(stream, normalizedKey, ContentType));
    }

    public Task<bool> DeleteAsync(string imageKey, CancellationToken cancellationToken)
    {
        if (!TryNormalizeKey(imageKey, out var normalizedKey))
        {
            return Task.FromResult(false);
        }

        var path = GetPath(normalizedKey);
        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    private async Task<byte[]> NormalizeToJpegAsync(
        byte[] imageBytes,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var image = Image.Load(imageBytes);

            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension)
                }));
            }

            await using var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 82 }, cancellationToken);
            return output.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid public announcement image upload. FileName={FileName}", fileName);
            throw new InvalidOperationException("Uploaded file is not a valid image.", ex);
        }
    }

    private string GetPath(string imageKey)
    {
        return Path.Combine(_rootPath, imageKey);
    }

    private static bool TryNormalizeKey(string imageKey, out string normalizedKey)
    {
        normalizedKey = Path.GetFileName(imageKey);
        return !string.IsNullOrWhiteSpace(normalizedKey)
            && normalizedKey.Equals(imageKey, StringComparison.Ordinal)
            && normalizedKey.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);
    }
}
