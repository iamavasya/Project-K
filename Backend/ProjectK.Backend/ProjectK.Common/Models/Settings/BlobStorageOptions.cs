namespace ProjectK.Common.Models.Settings;

/// <summary>
/// Options for both Azure Blob Storage and the Azurite emulator.
/// <para>
/// Lives in Common rather than beside the Azure client because the mapping profiles that build public
/// photo URLs need it, and BusinessLogic cannot see Infrastructure.
/// </para>
/// </summary>
public sealed class BlobStorageOptions
{
    /// <summary>
    /// Full connection string. Azurite accepts <c>UseDevelopmentStorage=true</c>, or an explicit
    /// <c>DefaultEndpointsProtocol=...;AccountName=...;AccountKey=...;BlobEndpoint=...</c>.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    public string ContainerName { get; init; } = "photos";

    public string? PublicBaseUrl { get; init; }

    public bool AutoCreateContainer { get; init; } = true;

    public bool PublicAccess { get; init; } = true;

    public string UsageMetadataKey { get; init; } = "inUse";
}
