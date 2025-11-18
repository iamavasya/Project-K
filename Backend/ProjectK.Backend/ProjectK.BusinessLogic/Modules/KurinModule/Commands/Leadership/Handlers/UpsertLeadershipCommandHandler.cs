using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Leadership.Handlers
{
    public class UpsertLeadershipCommandHandler : IRequestHandler<UpsertLeadershipCommand, ServiceResult<LeadershipDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpsertLeadershipCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<LeadershipDto>> Handle(UpsertLeadershipCommand request, CancellationToken cancellationToken)
        {
            Common.Entities.KurinModule.Leadership? existing = null;
            bool isCreated = false;
            if (request.LeadershipKey != null && request.LeadershipKey != Guid.Empty)
            {
                existing = await _unitOfWork.Leaderships.GetByKeyAsync(request.LeadershipKey!.Value, cancellationToken);
            }

            if (existing != null)
            {
                // Update existing Leadership
                _mapper.Map(request, existing);
                existing.Type = Enum.Parse<Common.Models.Enums.LeadershipType>(request.Type!);
                switch (existing.Type)
                {
                    case LeadershipType.Kurin:
                        existing.KurinKey = request.EntityKey;
                        existing.GroupKey = null;
                        break;
                    case LeadershipType.Group:
                        existing.GroupKey = request.EntityKey;
                        existing.KurinKey = null;
                        break;
                    case LeadershipType.KV:
                        existing.KurinKey = request.EntityKey;
                        existing.GroupKey = null;
                        break;
                }
                _unitOfWork.Leaderships.Update(existing, cancellationToken);
            }
            else
            {
                // Create new Leadership
                existing = _mapper.Map<Common.Entities.KurinModule.Leadership>(request);
                existing.Type = Enum.Parse<Common.Models.Enums.LeadershipType>(char.ToUpper(request.Type[0]) + request.Type.Substring(1));
                switch (existing.Type)
                {
                    case LeadershipType.Kurin:
                        existing.KurinKey = request.EntityKey;
                        existing.GroupKey = null;
                        break;
                    case LeadershipType.Group:
                        existing.GroupKey = request.EntityKey;
                        existing.KurinKey = null;
                        break;
                    case LeadershipType.KV:
                        existing.KurinKey = request.EntityKey;
                        existing.GroupKey = null;
                        break;
                }
                _unitOfWork.Leaderships.Add(existing, cancellationToken);
                isCreated = true;
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<LeadershipDto>(ResultType.InternalServerError);
            }

            var response = _mapper.Map<LeadershipDto>(existing);

            return isCreated
                ? new ServiceResult<LeadershipDto>(ResultType.Created, response, CreatedAtActionName: "GetLeadershipByKey", CreatedAtRouteValues: new { leadershipKey = response.LeadershipKey })
                : new ServiceResult<LeadershipDto>(ResultType.Success, response);
        }
    }
}
