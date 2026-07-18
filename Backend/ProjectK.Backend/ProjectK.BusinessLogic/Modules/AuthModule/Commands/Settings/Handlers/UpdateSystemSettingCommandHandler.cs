using MediatR;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Settings.Handlers
{
    public class UpdateSystemSettingCommandHandler : IRequestHandler<UpdateSystemSettingCommand, ServiceResult<object>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSystemSettingCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<object>> Handle(UpdateSystemSettingCommand request, CancellationToken cancellationToken)
        {
            var setting = await _unitOfWork.SystemSettings.GetByKeyAsync(request.Key, cancellationToken);
            if (setting == null)
            {
                setting = new SystemSetting { Key = request.Key, Value = request.Value, UpdatedAtUtc = DateTime.UtcNow };
                _unitOfWork.SystemSettings.Create(setting);
            }
            else
            {
                setting.Value = request.Value;
                setting.UpdatedAtUtc = DateTime.UtcNow;
                _unitOfWork.SystemSettings.Update(setting);
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new ServiceResult<object>(ResultType.Success, null);
        }
    }
}
