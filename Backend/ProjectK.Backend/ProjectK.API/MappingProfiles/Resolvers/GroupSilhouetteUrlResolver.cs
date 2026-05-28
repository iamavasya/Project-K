using AutoMapper;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Infrastructure.Services.BlobStorageService;

namespace ProjectK.API.MappingProfiles.Resolvers
{
    public sealed class GroupSilhouetteUrlResolver : IValueResolver<Group, GroupResponse, string?>
    {
        private readonly BlobStorageOptions _options;

        public GroupSilhouetteUrlResolver()
            : this(new BlobStorageOptions())
        {
        }

        public GroupSilhouetteUrlResolver(BlobStorageOptions options)
        {
            _options = options;
        }

        public string? Resolve(Group source, GroupResponse destination, string? destMember, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(source.SilhouetteBlobName))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            {
                return source.SilhouetteBlobName;
            }

            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{EncodeBlobPath(source.SilhouetteBlobName)}";
        }

        private static string EncodeBlobPath(string blobName)
            => string.Join("/", blobName
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }
}
