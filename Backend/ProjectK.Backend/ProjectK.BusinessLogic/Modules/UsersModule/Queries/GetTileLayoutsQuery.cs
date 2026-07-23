using MediatR;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Queries
{
    public record GetTileLayoutsQuery(Guid UserKey) : IRequest<ServiceResult<IReadOnlyList<TileLayoutDto>>>;
}
