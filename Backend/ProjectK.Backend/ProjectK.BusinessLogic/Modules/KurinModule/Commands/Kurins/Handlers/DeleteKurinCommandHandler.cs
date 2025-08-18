using MediatR;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Kurins.Handlers
{
    public class DeleteKurinCommandHandler : IRequestHandler<DeleteKurinCommand, ServiceResult<object>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeleteKurinCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ServiceResult<object>> Handle(DeleteKurinCommand request, CancellationToken cancellationToken)
        {
            if (request.KurinKey == Guid.Empty)
            {
                return new ServiceResult<object>(
                    ResultType.BadRequest,
                    "KurinKey cannot be empty.");
            }

            var existing = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);

            if (existing is null)
            {
                return new ServiceResult<object>(
                    ResultType.NotFound,
                    $"Kurin with key {request.KurinKey} not found.");
            }

            _unitOfWork.Kurins.Delete(existing, cancellationToken);

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<object>(
                    ResultType.InternalServerError,
                    "Failed to delete Kurin due to internal error.");
            }

            return new ServiceResult<object>(ResultType.Success);
        }
    }
}
