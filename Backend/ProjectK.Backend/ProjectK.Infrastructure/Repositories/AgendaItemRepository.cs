using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Common.Models.Authorization;

namespace ProjectK.Infrastructure.Repositories
{
    public class AgendaItemRepository : IAgendaItemRepository
    {
        private readonly AppDbContext _context;

        public AgendaItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(AgendaItem entity, CancellationToken cancellationToken = default)
        {
            _context.AgendaItems.Add(entity);
        }

        public void Update(AgendaItem entity, CancellationToken cancellationToken = default)
        {
            _context.AgendaItems.Update(entity);
        }

        public void Delete(AgendaItem entity, CancellationToken cancellationToken = default)
        {
            _context.AgendaItems.Remove(entity);
        }

        public async Task<AgendaItem?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.AgendaItems.FirstOrDefaultAsync(a => a.AgendaItemKey == entityKey, cancellationToken);
        }

        public async Task<AgendaItem?> GetByKeyWithAssignmentsAsync(Guid agendaItemKey, CancellationToken token = default)
        {
            return await _context.AgendaItems
                .Include(a => a.Assignments)
                .FirstOrDefaultAsync(a => a.AgendaItemKey == agendaItemKey, token);
        }

        public void AddAssignment(AgendaAssignment assignment)
        {
            _context.AgendaAssignments.Add(assignment);
        }

        public void RemoveAssignment(AgendaAssignment assignment)
        {
            _context.AgendaAssignments.Remove(assignment);
        }

        public async Task ClearCategoryAsync(Guid agendaCategoryKey, CancellationToken cancellationToken = default)
        {
            await _context.AgendaItems
                .Where(a => a.AgendaCategoryKey == agendaCategoryKey)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.AgendaCategoryKey, (Guid?)null), cancellationToken);
        }

        public Task<IEnumerable<AgendaItem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Use GetForViewerAsync instead.");
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.AgendaItems.AnyAsync(a => a.AgendaItemKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<AgendaItem>> GetForViewerAsync(
            AgendaViewerScope viewer,
            DateTime? fromUtc,
            DateTime? toUtc,
            bool onlyDated,
            AgendaItemKind? kind,
            CancellationToken token = default)
        {
            var query = _context.AgendaItems
                .Where(a => a.KurinKey == viewer.KurinKey);

            if (kind.HasValue)
            {
                query = query.Where(a => a.Kind == kind.Value);
            }

            if (onlyDated)
            {
                query = query.Where(a => a.StartUtc != null);
            }

            // A dated item overlaps the window when it starts before the window ends and finishes
            // after it begins; a single-day item (no EndUtc) is treated as ending at its start. Recurring
            // items skip this narrowing — their base date may sit outside the window while occurrences fall
            // inside — so the handler's expansion decides which instances land in range. We still drop a
            // recurring series once its recurrence-end is before the window, so ended series aren't fetched.
            if (fromUtc.HasValue)
            {
                query = query.Where(a =>
                    (a.RecurrenceFrequency != RecurrenceFrequency.None && (a.RecurrenceEndUtc == null || a.RecurrenceEndUtc >= fromUtc.Value))
                    || (a.RecurrenceFrequency == RecurrenceFrequency.None && ((a.EndUtc ?? a.StartUtc) == null || (a.EndUtc ?? a.StartUtc) >= fromUtc.Value)));
            }

            if (toUtc.HasValue)
            {
                // Occurrences (recurring or not) never start before the base start, so a series/item starting
                // after the window ends has nothing to show.
                query = query.Where(a => a.StartUtc == null || a.StartUtc <= toUtc.Value);
            }

            if (!viewer.CanSeeWholeKurin)
            {
                query = query.Where(AgendaVisibility.AssignedToViewer(viewer));
            }

            return await query
                .Include(a => a.Assignments)
                .AsNoTracking()
                .OrderBy(a => a.StartUtc)
                .ToListAsync(token);
        }
    }
}
