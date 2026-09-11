using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.AuthModule;

public sealed class AppUserRepository : IAppUserRepository
{
    private readonly AppDbContext _context;

    public AppUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Users.AsNoTracking().ToListAsync(cancellationToken);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<int> CountActiveAsync(
        IReadOnlyCollection<Guid> userKeys,
        CancellationToken cancellationToken = default)
    {
        if (userKeys.Count == 0)
        {
            return Task.FromResult(0);
        }

        var keys = userKeys.Distinct().ToList();
        return _context.Users.CountAsync(
            user => keys.Contains(user.Id) && user.OnboardingStatus == OnboardingStatus.Active,
            cancellationToken);
    }

    public Task<int> CountActiveBetaAsync(
        IReadOnlyCollection<Guid>? userKeys,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(
            user => user.IsBetaParticipant && user.OnboardingStatus == OnboardingStatus.Active);

        if (userKeys is not null)
        {
            if (userKeys.Count == 0)
            {
                return Task.FromResult(0);
            }

            var keys = userKeys.Distinct().ToList();
            query = query.Where(user => keys.Contains(user.Id));
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task DetachFromKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default)
    {
        var scoped = await _context.Users
            .Where(user => user.ActiveKurinKey == kurinKey || user.KurinKey == kurinKey)
            .ToListAsync(cancellationToken);

        foreach (var user in scoped)
        {
            if (user.ActiveKurinKey == kurinKey)
            {
                user.ActiveKurinKey = null;
            }

            if (user.KurinKey == kurinKey)
            {
                user.KurinKey = null;
            }
        }
    }
}
