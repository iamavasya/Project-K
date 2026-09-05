using MediatR;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Dtos.UsersModule;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.TileLayout.Save
{
    public record SaveTileLayoutCommand(
        Guid UserKey,
        string BoardKey,
        IReadOnlyList<string> TileKeys,
        int SchemaVersion) : IRequest<ServiceResult<TileLayoutDto>>;
}
