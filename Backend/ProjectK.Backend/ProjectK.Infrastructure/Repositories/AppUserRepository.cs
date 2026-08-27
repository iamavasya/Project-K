using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories;

public sealed class AppUserRepository : IAppUserRepository
{
    private readonly AppDbContext _context;

    public AppUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Users.AsNoTracking().ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        => _context.Users.CountAsync(
            user => user.KurinKey == kurinKey && user.OnboardingStatus == OnboardingStatus.Active,
            cancellationToken);

    public Task<int> CountActiveBetaAsync(Guid? kurinKey, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(
            user => user.IsBetaParticipant && user.OnboardingStatus == OnboardingStatus.Active);

        if (kurinKey.HasValue)
        {
            query = query.Where(user => user.KurinKey == kurinKey.Value);
        }

        return query.CountAsync(cancellationToken);
    }
}
