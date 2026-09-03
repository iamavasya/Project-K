using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.AuthModule;

/// <inheritdoc />
public sealed class RefreshTokenStore : IRefreshTokenStore
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenStore(AppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task IssueAsync(
        Guid userId,
        string token,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        _context.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<UserRefreshToken?> FindActiveAsync(string token, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return _context.UserRefreshTokens
            .FirstOrDefaultAsync(
                session => session.Token == token
                    && session.RevokedAtUtc == null
                    && session.ExpiresAtUtc > now,
                cancellationToken);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        await _context.UserRefreshTokens
            .Where(session => session.Token == token && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                session => session.SetProperty(row => row.RevokedAtUtc, now),
                cancellationToken);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        await _context.UserRefreshTokens
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                session => session.SetProperty(row => row.RevokedAtUtc, now),
                cancellationToken);
    }
}
