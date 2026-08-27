using MediatR;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Queries;

/// <summary>How many stored announcement images no draft references any more.</summary>
public sealed record GetPublicAnnouncementCleanupStatusQuery
    : IRequest<ServiceResult<PublicAnnouncementCleanupStatusDto>>;
