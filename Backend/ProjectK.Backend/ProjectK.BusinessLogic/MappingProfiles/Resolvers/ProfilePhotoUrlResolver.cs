using ProjectK.Common.Models.Records;
using AutoMapper;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Settings;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.BusinessLogic.MappingProfiles.Resolvers
{
    // Turns a stored blob name into a public URL. Shared by the full member card
    // (from the entity) and the lean member list (from the projected read model)
    // so both build the same URL from one place.
    internal static class ProfilePhotoUrl
    {
        public static string? Build(BlobStorageOptions options, string? blobName)
            => BlobPublicUrl.Build(options.PublicBaseUrl, blobName);
    }

    public sealed class ProfilePhotoUrlResolver : IValueResolver<Member, MemberResponse, string?>
    {
        private readonly BlobStorageOptions _options;

        public ProfilePhotoUrlResolver(BlobStorageOptions options)
        {
            _options = options;
        }

        public string? Resolve(Member source, MemberResponse destination, string? destMember, ResolutionContext context)
            => ProfilePhotoUrl.Build(_options, source.ProfilePhotoBlobName);
    }

    public sealed class MemberListItemPhotoUrlResolver : IValueResolver<MemberListItemDto, MemberResponse, string?>
    {
        private readonly BlobStorageOptions _options;

        public MemberListItemPhotoUrlResolver(BlobStorageOptions options)
        {
            _options = options;
        }

        public string? Resolve(MemberListItemDto source, MemberResponse destination, string? destMember, ResolutionContext context)
            => ProfilePhotoUrl.Build(_options, source.ProfilePhotoBlobName);
    }
}
