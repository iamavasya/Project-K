using AutoMapper;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;

namespace ProjectK.BusinessLogic.MappingProfiles.Resolvers;

public sealed class GroupSilhouetteUrlResolver : IValueResolver<Group, GroupResponse, string?>
{
    private readonly IBlobReadLinks _links;

    public GroupSilhouetteUrlResolver(IBlobReadLinks links)
    {
        _links = links;
    }

    public string? Resolve(Group source, GroupResponse destination, string? destMember, ResolutionContext context)
        => _links.For(source.SilhouetteBlobName);
}
