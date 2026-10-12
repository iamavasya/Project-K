using AutoMapper;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.MappingProfiles.Resolvers;

public sealed class ProfilePhotoUrlResolver : IValueResolver<Member, MemberResponse, string?>
{
    private readonly IBlobReadLinks _links;

    public ProfilePhotoUrlResolver(IBlobReadLinks links)
    {
        _links = links;
    }

    public string? Resolve(Member source, MemberResponse destination, string? destMember, ResolutionContext context)
        => _links.For(source.ProfilePhotoBlobName);
}

public sealed class MemberListItemPhotoUrlResolver : IValueResolver<MemberListItemDto, MemberResponse, string?>
{
    private readonly IBlobReadLinks _links;

    public MemberListItemPhotoUrlResolver(IBlobReadLinks links)
    {
        _links = links;
    }

    public string? Resolve(MemberListItemDto source, MemberResponse destination, string? destMember, ResolutionContext context)
        => _links.For(source.ProfilePhotoBlobName);
}

public sealed class MemberLookupPhotoUrlResolver : IValueResolver<Member, MemberLookupDto, string?>
{
    private readonly IBlobReadLinks _links;

    public MemberLookupPhotoUrlResolver(IBlobReadLinks links)
    {
        _links = links;
    }

    public string? Resolve(Member source, MemberLookupDto destination, string? destMember, ResolutionContext context)
        => _links.For(source.ProfilePhotoBlobName);
}

public sealed class MemberSummaryLookupPhotoUrlResolver : IValueResolver<MemberSummary, MemberLookupDto, string?>
{
    private readonly IBlobReadLinks _links;

    public MemberSummaryLookupPhotoUrlResolver(IBlobReadLinks links)
    {
        _links = links;
    }

    public string? Resolve(MemberSummary source, MemberLookupDto destination, string? destMember, ResolutionContext context)
        => _links.For(source.ProfilePhotoBlobName);
}
