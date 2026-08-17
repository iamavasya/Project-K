using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface ILeadershipRepository
    {
        Task<Leadership?> GetByKeyAsync(Guid leadershipKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<Leadership>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Leadership>> GetAllByTypeAsync(LeadershipType type, Guid entityKey, CancellationToken cancellationToken = default);

        void Add(Leadership leadership, CancellationToken cancellationToken = default);
        void Update(Leadership leadership, CancellationToken cancellationToken = default);
        Task CloseLeadershipAsync(Guid leadershipKey, DateOnly endDate, CancellationToken cancellationToken = default);

        Task<IEnumerable<LeadershipHistory>> GetLeadershipHistoriesAsync(Guid leadershipKey, CancellationToken cancellationToken = default);
        void LeadershipHistoriesRemoveRange(IEnumerable<LeadershipHistory> histories);

        /// <summary>The offices a member currently holds (active <see cref="LeadershipHistory"/>), each with its провід.</summary>
        Task<IReadOnlyList<MemberOffice>> GetActiveOfficesForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Member keys holding an active office in one of <paramref name="roles"/>, optionally scoped to a
        /// kurin (курінний/КВ offices) or a specific group (гуртковий offices).
        /// </summary>
        Task<IReadOnlyList<Guid>> GetActiveOfficeMemberKeysAsync(
            IReadOnlyCollection<LeadershipRole> roles,
            Guid? kurinKey = null,
            Guid? groupKey = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Members currently holding a kurin- or КВ-scoped office in the kurin, one lookup row per office,
        /// with <c>UserRole</c> set to the office role name. Used to render the КВ / провід tables.
        /// </summary>
        Task<IReadOnlyList<ProjectK.Common.Models.Dtos.MemberLookupDto>> GetOfficeMembersLookupAsync(
            Guid kurinKey,
            LeadershipType type,
            CancellationToken cancellationToken = default);

        /// <summary>Every провід/КВ that belongs to the kurin (курінь/КВ by kurin, гуртковий by its group).</summary>
        Task<IReadOnlyList<LeadershipRef>> GetLeadershipRefsForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);

        /// <summary>User keys of the members who currently hold an office in the given провід (active history).</summary>
        Task<IReadOnlyList<Guid>> GetActiveMemberUserKeysForLeadershipAsync(Guid leadershipKey, CancellationToken cancellationToken = default);

        /// <summary>The провід keys a member currently belongs to (active <see cref="LeadershipHistory"/>).</summary>
        Task<IReadOnlyList<Guid>> GetActiveLeadershipKeysForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default);
    }
}
