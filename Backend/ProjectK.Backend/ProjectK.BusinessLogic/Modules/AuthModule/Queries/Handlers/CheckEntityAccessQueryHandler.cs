using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Handlers
{
    public class CheckEntityAccessQueryHandler : IRequestHandler<CheckEntityAccessQuery, ServiceResult<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public CheckEntityAccessQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ServiceResult<bool>> Handle(CheckEntityAccessQuery request, CancellationToken cancellationToken)
        {
            Guid.TryParse(request.EntityKey, out var parsedEntityKey);
            switch (request.EntityType.ToLower())
            {
                case "group":
                    var group = await _unitOfWork.Groups.GetByKeyAsync(parsedEntityKey, cancellationToken);
                    if (group?.KurinKey.ToString() == request.ActiveKurinKey)
                    {
                        return new ServiceResult<bool>(ResultType.Success, true);
                    }
                    return new ServiceResult<bool>(ResultType.Success, false);

                case "member":
                    var member = await _unitOfWork.Members.GetByKeyAsync(parsedEntityKey, cancellationToken);
                    if (member?.KurinKey.ToString() == request.ActiveKurinKey)
                    {
                        return new ServiceResult<bool>(ResultType.Success, true);
                    }
                    return new ServiceResult<bool>(ResultType.Success, false);
                
                default:
                    return new ServiceResult<bool>(ResultType.BadRequest, false, "Invalid entity type.");
            }
        }
    }
}
