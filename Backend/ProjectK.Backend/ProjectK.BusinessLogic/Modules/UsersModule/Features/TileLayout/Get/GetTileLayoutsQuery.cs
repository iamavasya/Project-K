using MediatR;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.TileLayout.Get
{
    public record GetTileLayoutsQuery(Guid UserKey) : IRequest<ServiceResult<IReadOnlyList<TileLayoutDto>>>;
}
