using Microsoft.Extensions.Logging;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services
{
    public class ResourceAccessServiceInstrumentationDecorator : IResourceAccessService
    {
        private readonly IResourceAccessService _inner;
        private readonly ILogger<ResourceAccessServiceInstrumentationDecorator> _logger;

        public ResourceAccessServiceInstrumentationDecorator(
            IResourceAccessService inner,
            ILogger<ResourceAccessServiceInstrumentationDecorator> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<ResourceAccessDecision> CheckAccessAsync(
            ResourceType resourceType,
            ResourceAction action,
            Guid resourceKey,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            
            // Note: Query count instrumentation would ideally be handled at the UnitOfWork/DbContext level,
            // but for this baseline we can log the high-level latency per resource type.
            var decision = await _inner.CheckAccessAsync(resourceType, action, resourceKey, cancellationToken);
            
            sw.Stop();

            _logger.LogInformation(
                "Access check for {ResourceType} {Action} on {ResourceKey} took {ElapsedMs}ms. Decision: {IsAllowed}. Reason: {Reason}",
                resourceType, action, resourceKey, sw.ElapsedMilliseconds, decision.IsAllowed, decision.Reason);

            return decision;
        }
    }
}
