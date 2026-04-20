using ProjectK.Common.Entities.KurinModule;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IMentorAssignmentRepository : IBaseEntityRepository<MentorAssignment>
    {
        Task<IEnumerable<MentorAssignment>> GetByMentorUserKeyAsync(Guid mentorUserKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<MentorAssignment>> GetByGroupKeyAsync(Guid groupKey, CancellationToken cancellationToken = default);
        Task<MentorAssignment?> GetSpecificAssignmentAsync(Guid mentorUserKey, Guid groupKey, CancellationToken cancellationToken = default);
    }
}
