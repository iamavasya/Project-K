using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Delete
{
    public class DeleteKurin : IRequest<ServiceResult<object>>
    {
        public Guid KurinKey { get; set; }
        public DeleteKurin(Guid kurinKey)
        {
            KurinKey = kurinKey;
        }
    }

    public class DeleteKurinHandler : IRequestHandler<DeleteKurin, ServiceResult<object>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeleteKurinHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ServiceResult<object>> Handle(DeleteKurin request, CancellationToken cancellationToken)
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

            // Delete all members with KurinKey
            var members = await _unitOfWork.Members.GetAllByKurinKeyAsync(request.KurinKey, cancellationToken);

            foreach (var member in members)
            {
                member.Group = null;
                member.Kurin = null!;
                _unitOfWork.Members.Delete(member, cancellationToken);
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
