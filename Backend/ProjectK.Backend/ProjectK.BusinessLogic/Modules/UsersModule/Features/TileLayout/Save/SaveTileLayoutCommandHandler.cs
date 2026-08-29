using MediatR;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.TileLayout;
using ProjectK.Common.Models.Dtos.UsersModule;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.TileLayout.Save
{
    public class SaveTileLayoutCommandHandler : IRequestHandler<SaveTileLayoutCommand, ServiceResult<TileLayoutDto>>
    {
        private const int MaxOrderJsonLength = 2000;

        private readonly IUnitOfWork _unitOfWork;

        public SaveTileLayoutCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<TileLayoutDto>> Handle(SaveTileLayoutCommand request, CancellationToken cancellationToken)
        {
            // Input validation lives in SaveTileLayoutCommandValidator (runs in the pipeline).
            var tileKeys = request.TileKeys ?? [];

            var orderJson = TileOrderSerializer.Serialize(tileKeys);
            if (orderJson.Length > MaxOrderJsonLength)
            {
                return ServiceResult<TileLayoutDto>.Failure(ResultType.BadRequest, "LayoutTooLarge", "Tile layout is too large.");
            }

            var schemaVersion = request.SchemaVersion <= 0 ? 1 : request.SchemaVersion;
            var existing = await _unitOfWork.UserTileLayouts.GetByBoardAsync(request.UserKey, request.BoardKey, cancellationToken);

            if (existing == null)
            {
                existing = new UserTileLayout
                {
                    UserTileLayoutKey = Guid.NewGuid(),
                    UserKey = request.UserKey,
                    BoardKey = request.BoardKey,
                    TileOrderJson = orderJson,
                    SchemaVersion = schemaVersion,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _unitOfWork.UserTileLayouts.Create(existing, cancellationToken);
            }
            else
            {
                existing.TileOrderJson = orderJson;
                existing.SchemaVersion = schemaVersion;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                _unitOfWork.UserTileLayouts.Update(existing, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = new TileLayoutDto(existing.BoardKey, tileKeys, existing.SchemaVersion, existing.UpdatedAtUtc);
            return new ServiceResult<TileLayoutDto>(ResultType.Success, dto);
        }
    }
}
