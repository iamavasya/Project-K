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
    /// <summary>
    /// The assignment of one mentor to one group. A pair can hold several rows, because revoking
    /// keeps the old one as history: the active assignment wins, otherwise the latest revoked one.
    /// </summary>
    Task<MentorAssignment?> GetSpecificAssignmentAsync(Guid mentorUserKey, Guid groupKey, CancellationToken cancellationToken = default);
}
