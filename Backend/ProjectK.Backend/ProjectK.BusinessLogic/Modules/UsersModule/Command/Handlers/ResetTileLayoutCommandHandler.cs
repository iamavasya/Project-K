using MediatR;
using ProjectK.Common.Interfaces;
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
            // Board key validation lives in ResetTileLayoutCommandValidator (runs in the pipeline).
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
