using Microsoft.EntityFrameworkCore.Storage;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces
{
    public interface IUnitOfWork
    {
        IKurinRepository Kurins { get; }
        IGroupRepository Groups { get; }
        IMemberRepository Members { get; }
        ILeadershipRepository Leaderships { get; }
        IPlanningSessionRepository PlanningSessions { get; }
        IBadgeProgressRepository BadgeProgresses { get; }
        IProbeProgressRepository ProbeProgresses { get; }
        Task<int> SaveChangesAsync(CancellationToken token = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default);
        void DetectChanges();
    }
}
