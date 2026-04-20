using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
{
    public class MentorAssignmentRepository : IMentorAssignmentRepository
    {
        private readonly AppDbContext _context;

        public MentorAssignmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(MentorAssignment entity, CancellationToken cancellationToken = default)
        {
            _context.MentorAssignments.Add(entity);
        }

        public void Delete(MentorAssignment entity, CancellationToken cancellationToken = default)
        {
            _context.MentorAssignments.Remove(entity);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.MentorAssignments.AnyAsync(ma => ma.MentorAssignmentKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<MentorAssignment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MentorAssignments.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<MentorAssignment>> GetByGroupKeyAsync(Guid groupKey, CancellationToken cancellationToken = default)
        {
            return await _context.MentorAssignments
                .Where(ma => ma.GroupKey == groupKey)
                .ToListAsync(cancellationToken);
        }

        public async Task<MentorAssignment?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.MentorAssignments
                .FirstOrDefaultAsync(ma => ma.MentorAssignmentKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<MentorAssignment>> GetByMentorUserKeyAsync(Guid mentorUserKey, CancellationToken cancellationToken = default)
        {
            return await _context.MentorAssignments
                .Where(ma => ma.MentorUserKey == mentorUserKey)
                .ToListAsync(cancellationToken);
        }

        public async Task<MentorAssignment?> GetSpecificAssignmentAsync(Guid mentorUserKey, Guid groupKey, CancellationToken cancellationToken = default)
        {
            return await _context.MentorAssignments
                .FirstOrDefaultAsync(ma => ma.MentorUserKey == mentorUserKey && ma.GroupKey == groupKey, cancellationToken);
        }

        public void Update(MentorAssignment entity, CancellationToken cancellationToken = default)
        {
            _context.MentorAssignments.Update(entity);
        }
    }
}
