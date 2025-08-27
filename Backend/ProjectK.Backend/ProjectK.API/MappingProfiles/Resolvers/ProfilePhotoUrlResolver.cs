using AutoMapper;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Infrastructure.Services;

namespace ProjectK.API.MappingProfiles.Resolvers
{
    internal sealed class ProfilePhotoUrlResolver : IValueResolver<Member, MemberResponse, string?>
    {
        private readonly BlobStorageOptions _options;

        public ProfilePhotoUrlResolver(BlobStorageOptions options)
        {
            _options = options;
        }

        public string? Resolve(Member source, MemberResponse destination, string? destMember, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source.ProfilePhotoBlobName))
                return null;

            if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
                return $"{_options.PublicBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(source.ProfilePhotoBlobName)}";

            return source.ProfilePhotoBlobName;
        }
    }
}