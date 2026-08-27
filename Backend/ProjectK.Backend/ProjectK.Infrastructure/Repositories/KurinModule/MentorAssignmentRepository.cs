using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories.KurinModule
{
    public class MentorAssignmentRepository : BaseEntityRepository<MentorAssignment>, IMentorAssignmentRepository
    {

        public MentorAssignmentRepository(AppDbContext context) : base(context)
        {
        }

        public override async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await Context.MentorAssignments.AnyAsync(ma => ma.MentorAssignmentKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<MentorAssignment>> GetByGroupKeyAsync(Guid groupKey, CancellationToken cancellationToken = default)
        {
            return await Context.MentorAssignments
                .Where(ma => ma.GroupKey == groupKey)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<MentorAssignment>> GetByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            return await Context.MentorAssignments
                .Include(ma => ma.Group)
                .Where(ma => ma.Group.KurinKey == kurinKey)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public override async Task<MentorAssignment?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await Context.MentorAssignments
                .FirstOrDefaultAsync(ma => ma.MentorAssignmentKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<MentorAssignment>> GetByMentorUserKeyAsync(Guid mentorUserKey, CancellationToken cancellationToken = default)
        {
            return await Context.MentorAssignments
                .Where(ma => ma.MentorUserKey == mentorUserKey)
                .ToListAsync(cancellationToken);
        }

        public async Task<MentorAssignment?> GetSpecificAssignmentAsync(Guid mentorUserKey, Guid groupKey, CancellationToken cancellationToken = default)
        {
            return await Context.MentorAssignments
                .FirstOrDefaultAsync(ma => ma.MentorUserKey == mentorUserKey && ma.GroupKey == groupKey, cancellationToken);
        }

    }
}
