using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectK.Common.Entities.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IMemberRepository : IBaseEntityRepository<Member>
    {
        Task<IEnumerable<Member>> GetAllAsync(Guid groupKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<Member>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Everyone the kurin holds, as tracked entities without their graph — for deleting them.
        /// <para>
        /// Matches on the kurin <b>or</b> on membership of one of its гуртки. <c>KurinKey</c> and
        /// <c>GroupKey</c> are independent columns with nothing tying them together, and it is
        /// <c>Members -> Groups</c> that blocks the гуртки's cascade — so selecting by kurin alone
        /// would miss a member whose two keys disagree and fail the delete on a raw foreign key.
        /// </para>
        /// <para>
        /// <see cref="GetAllByKurinKeyAsync"/> eager-loads <c>AsNoTracking</c> for the screens that read
        /// it, which is poison for a delete: every member carries its own detached kurin, and removing
        /// one puts a second copy of the already-tracked kurin in front of EF.
        /// </para>
        /// </summary>
        Task<IEnumerable<Member>> GetTrackedForKurinDeletionAsync(Guid kurinKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<MemberListItemDto>> GetListItemsByKurinKeyAsync(Guid kurinKey, MemberFieldVisibility visibility, CancellationToken cancellationToken = default);
        Task<IEnumerable<MemberListItemDto>> GetListItemsByGroupKeyAsync(Guid groupKey, MemberFieldVisibility visibility, CancellationToken cancellationToken = default);
        Task<IEnumerable<MemberLookupDto>> GetMentorCandidatesLookupAsync(Guid kurinKey, CancellationToken cancellationToken = default);
        Task<Member?> GetByUserKeyAsync(Guid userKey, CancellationToken cancellationToken = default);
        // Narrow reads for handlers that only need one field — avoids loading the full
        // Include graph of GetByKeyAsync just to read a single key. Null means no such member.
        Task<Guid?> GetUserKeyByMemberAsync(Guid memberKey, CancellationToken cancellationToken = default);
        Task<Guid?> GetKurinKeyByMemberAsync(Guid memberKey, CancellationToken cancellationToken = default);
        Task<Member?> GetTrackedByUserKeyAsync(Guid userKey, CancellationToken cancellationToken = default);
        Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        #region PlastLevelHistory Methods
        #endregion

        #region LeadershipHistory Methods

        #endregion
    }
}
