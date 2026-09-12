using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

public interface IMentorAssignmentRepository : IBaseEntityRepository<MentorAssignment>
{
    Task<IEnumerable<MentorAssignment>> GetByMentorUserKeyAsync(Guid mentorUserKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<MentorAssignment>> GetByGroupKeyAsync(Guid groupKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<MentorAssignment>> GetByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default);
    Task<MentorAssignment?> GetSpecificAssignmentAsync(Guid mentorUserKey, Guid groupKey, CancellationToken cancellationToken = default);
}
