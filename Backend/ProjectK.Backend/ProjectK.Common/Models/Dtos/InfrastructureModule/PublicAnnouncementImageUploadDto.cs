namespace ProjectK.Common.Models.Dtos.InfrastructureModule;

/// <summary>
/// What an announcement image upload answers with: the key the draft stores and the URL the editor
/// renders. Named rather than anonymous so the OpenAPI spec carries its schema.
/// </summary>
public sealed record PublicAnnouncementImageUploadDto(string ImageBlobKey, string? ImageUrl);
