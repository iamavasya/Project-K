using ProjectK.Common.Models.Records;
using AutoMapper;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Settings;

namespace ProjectK.BusinessLogic.MappingProfiles.Resolvers
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

            return BlobPublicUrl.Build(_options.PublicBaseUrl, source.SilhouetteBlobName);
        }
    }
}
