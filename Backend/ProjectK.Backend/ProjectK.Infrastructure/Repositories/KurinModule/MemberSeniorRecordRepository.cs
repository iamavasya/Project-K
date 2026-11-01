using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.KurinModule;

public class MemberSeniorRecordRepository : BaseEntityRepository<MemberSeniorRecord>, IMemberSeniorRecordRepository
{
    public MemberSeniorRecordRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<MemberSeniorRecord?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await Context.MemberSeniorRecords
            .AsTracking()
            .FirstOrDefaultAsync(x => x.MemberSeniorRecordKey == entityKey, cancellationToken);
    }

    public override async Task<IEnumerable<MemberSeniorRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.MemberSeniorRecords
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public override async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await Context.MemberSeniorRecords
            .AnyAsync(x => x.MemberSeniorRecordKey == entityKey, cancellationToken);
    }

    public override void Update(MemberSeniorRecord entity, CancellationToken cancellationToken = default) => MarkModified(entity);

    public async Task<IReadOnlyCollection<MemberSeniorRecord>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
    {
        return await Context.MemberSeniorRecords
            .Where(x => x.MemberKey == memberKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
