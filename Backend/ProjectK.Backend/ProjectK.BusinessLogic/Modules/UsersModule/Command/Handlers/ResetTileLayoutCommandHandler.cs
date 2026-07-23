using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers
{
    public class ResetTileLayoutCommandHandler : IRequestHandler<ResetTileLayoutCommand, ServiceResult<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ResetTileLayoutCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<bool>> Handle(ResetTileLayoutCommand request, CancellationToken cancellationToken)
        {
            if (!TileBoardKeys.All.Contains(request.BoardKey))
            {
                return ServiceResult<bool>.Failure(ResultType.BadRequest, "UnknownBoard", "Unknown board key.");
            }

            var existing = await _unitOfWork.UserTileLayouts.GetByBoardAsync(request.UserKey, request.BoardKey, cancellationToken);
            if (existing != null)
            {
                _unitOfWork.UserTileLayouts.Delete(existing, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
