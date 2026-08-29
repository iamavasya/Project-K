using MediatR;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.TileLayout.Save
{
    public record SaveTileLayoutCommand(
        Guid UserKey,
        string BoardKey,
        IReadOnlyList<string> TileKeys,
        int SchemaVersion) : IRequest<ServiceResult<TileLayoutDto>>;
}
