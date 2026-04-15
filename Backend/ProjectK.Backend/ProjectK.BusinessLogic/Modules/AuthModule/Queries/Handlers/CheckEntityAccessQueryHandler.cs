using MediatR;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
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
        private readonly IResourceAccessService _resourceAccessService;

        public CheckEntityAccessQueryHandler(IResourceAccessService resourceAccessService)
        {
            _resourceAccessService = resourceAccessService;
        }

        public async Task<ServiceResult<bool>> Handle(CheckEntityAccessQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.EntityKey, out var parsedEntityKey))
            {
                return new ServiceResult<bool>(ResultType.BadRequest, false, "Invalid entity key.");
            }

            if (!TryMapResourceType(request.EntityType, out var resourceType))
            {
                return new ServiceResult<bool>(ResultType.BadRequest, false, "Invalid entity type.");
            }

            var decision = await _resourceAccessService.CheckAccessAsync(
                resourceType,
                ResourceAction.Read,
                parsedEntityKey,
                cancellationToken);

            return new ServiceResult<bool>(ResultType.Success, decision.IsAllowed);
        }

        private static bool TryMapResourceType(string entityType, out ResourceType resourceType)
        {
            if (Enum.TryParse<ResourceType>(entityType, ignoreCase: true, out resourceType))
            {
                return true;
            }

            return entityType.ToLowerInvariant() switch
            {
                "group" => Map(ResourceType.Group, out resourceType),
                "member" => Map(ResourceType.Member, out resourceType),
                "kurin" => Map(ResourceType.Kurin, out resourceType),
                "kv" => Map(ResourceType.Kurin, out resourceType),
                _ => Map(default, out resourceType, false)
            };
        }

        private static bool Map(ResourceType type, out ResourceType mapped, bool result = true)
        {
            mapped = type;
            return result;
        }
    }
}
