using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Services.Caching;
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
        private readonly IBackendCache _cache;
        public DeleteKurinHandler(IUnitOfWork unitOfWork, IBackendCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }
        public async Task<ServiceResult<object>> Handle(DeleteKurin request, CancellationToken cancellationToken)
        {
            if (request.KurinKey == Guid.Empty)
            {
                return ServiceResult<object>.Failure(
                    ResultType.BadRequest,
                    "KurinKeyRequired",
                    "KurinKey cannot be empty.");
            }

            var existing = await _unitOfWork.Kurins.GetByKeyAsync(request.KurinKey, cancellationToken);

            if (existing is null)
            {
                return ServiceResult<object>.Failure(
                    ResultType.NotFound,
                    "KurinNotFound",
                    $"Kurin with key {request.KurinKey} not found.");
            }

            // What the database will not clear itself: offices and members are NO ACTION against both
            // the kurin and its гуртки, and the гуртки's own cascade is refused while an office still
            // points at one. Everything else — гуртки, agenda with its assignments, planning sessions,
            // mentor assignments, the members' histories — cascades.
            await _unitOfWork.Leaderships.DeleteForKurinAsync(request.KurinKey, cancellationToken);

            var members = await _unitOfWork.Members.GetTrackedForKurinDeletionAsync(request.KurinKey, cancellationToken);

            foreach (var member in members)
            {
                _unitOfWork.Members.Delete(member, cancellationToken);
            }

            _unitOfWork.Kurins.Delete(existing, cancellationToken);

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return ServiceResult<object>.Failure(
                    ResultType.InternalServerError,
                    "KurinDeleteFailed",
                    "Failed to delete Kurin due to internal error.");
            }

            _cache.Invalidate(BackendCachePolicies.KurinReads);
            _cache.Invalidate(BackendCachePolicies.GroupReads);

            return new ServiceResult<object>(ResultType.Success);
        }
    }
}
