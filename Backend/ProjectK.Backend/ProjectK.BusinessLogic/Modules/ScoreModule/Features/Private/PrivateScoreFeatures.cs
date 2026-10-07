using FluentValidation;
using MediatR;
using ProjectK.BusinessLogic.Modules.ScoreModule.Models;
using ProjectK.BusinessLogic.Modules.ScoreModule.Services;
using ProjectK.Common.Entities.ScoreModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.ScoreModule;
using ProjectK.Common.Models.Dtos.ScoreModule.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ScoreModule.Features.Private;

// The КВ's own book. The endpoint lets in only those with KurinScorePrivate; here it is checked once
// more against the kurin the caller acts in, as every score handler does.

/// <summary>Who stands in the book — the Звʼязковий and the впорядники — and that this is their kurin.</summary>
public sealed class PrivateScoreAccess
{
    private readonly ScoreAccess _access;
    private readonly IResourceAccessService _resourceAccess;

    public PrivateScoreAccess(ScoreAccess access, IResourceAccessService resourceAccess)
    {
        _access = access;
        _resourceAccess = resourceAccess;
    }

    public async Task<ServiceResult<T>?> RefuseAsync<T>(Guid kurinKey, ResourceAction action, CancellationToken cancellationToken)
    {
        if (_access.Refuse<T>(kurinKey) is { } refused)
        {
            return refused;
        }

        var decision = await _resourceAccess.CheckAccessAsync(ResourceType.KurinScorePrivate, action, kurinKey, cancellationToken);
        return decision.IsAllowed ? null : ScoreAccess.Forbidden<T>();
    }
}

public sealed record GetPrivateScoreQuery(Guid KurinKey, ScorePeriodQuery Period) : IRequest<ServiceResult<PrivateScoreResponse>>;

public sealed class GetPrivateScoreQueryHandler : IRequestHandler<GetPrivateScoreQuery, ServiceResult<PrivateScoreResponse>>
{
    private readonly PrivateScoreAccess _access;
    private readonly ScoreBookReader _books;
    private readonly IScoreUnitOfWork _score;

    public GetPrivateScoreQueryHandler(PrivateScoreAccess access, ScoreBookReader books, IScoreUnitOfWork score)
    {
        _access = access;
        _books = books;
        _score = score;
    }

    public async Task<ServiceResult<PrivateScoreResponse>> Handle(GetPrivateScoreQuery request, CancellationToken cancellationToken)
    {
        if (await _access.RefuseAsync<PrivateScoreResponse>(request.KurinKey, ResourceAction.Read, cancellationToken) is { } refused)
        {
            return refused;
        }

        var book = await _books.OpenAsync(request.KurinKey, cancellationToken);
        if (book.ResolvePeriod(request.Period) is not { } resolved)
        {
            return ScoreAccess.NotFound<PrivateScoreResponse>("There is no such stage.");
        }

        var criteria = await _score.PrivateScoreCriteria.GetForKurinAsync(request.KurinKey, cancellationToken);
        var names = criteria.ToDictionary(c => c.PrivateScoreCriterionKey, c => c.Name);
        var entries = (await _score.PrivateScoreEntries.GetForKurinAsync(request.KurinKey, cancellationToken))
            .Where(e => resolved.Period.Contains(e.OccurredOn))
            .ToList();
        var publicTotals = book.Ledger.People(resolved.Period).ToDictionary(p => p.MembershipKey, p => p.Total);
        var byPerson = entries.ToLookup(e => e.MembershipKey);

        var people = book.CurrentYouths()
            .Select(y => new PrivateScorePersonDto
            {
                MembershipKey = y.Membership.MembershipKey,
                MemberKey = y.Membership.MemberKey,
                FullName = y.Person.FullName,
                GroupKey = y.Membership.GroupKey,
                GroupName = book.GroupName(y.Membership.GroupKey),
                PublicTotal = publicTotals.GetValueOrDefault(y.Membership.MembershipKey),
                PrivateTotal = byPerson[y.Membership.MembershipKey].Sum(e => e.Points),
                ByCriterion = byPerson[y.Membership.MembershipKey]
                    .Where(e => e.PrivateScoreCriterionKey.HasValue)
                    .GroupBy(e => e.PrivateScoreCriterionKey!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(e => e.Points)),
                Uncategorised = byPerson[y.Membership.MembershipKey].Where(e => e.PrivateScoreCriterionKey is null).Sum(e => e.Points)
            })
            .OrderByDescending(p => p.PrivateTotal)
            .ThenBy(p => p.FullName)
            .ToList();

        return new ServiceResult<PrivateScoreResponse>(ResultType.Success, new PrivateScoreResponse
        {
            KurinKey = request.KurinKey,
            Period = resolved.Dto,
            Periods = book.Periods(),
            Criteria = criteria.Select(c => new PrivateScoreCriterionDto { PrivateScoreCriterionKey = c.PrivateScoreCriterionKey, Name = c.Name, IsArchived = c.IsArchived }).ToList(),
            People = people,
            Entries = entries
                .OrderByDescending(e => e.OccurredOn)
                .ThenByDescending(e => e.CreatedDate)
                .Select(e => new PrivateScoreEntryDto
                {
                    PrivateScoreEntryKey = e.PrivateScoreEntryKey,
                    MembershipKey = e.MembershipKey,
                    MemberKey = book.MemberKeyOf(e.MembershipKey) ?? Guid.Empty,
                    MemberName = book.NameOfMembership(e.MembershipKey),
                    PrivateScoreCriterionKey = e.PrivateScoreCriterionKey,
                    CriterionName = e.PrivateScoreCriterionKey is { } ck ? names.GetValueOrDefault(ck) : null,
                    Points = e.Points,
                    Note = e.Note,
                    OccurredOn = e.OccurredOn,
                    CreatedByName = book.NameOfAccount(e.CreatedByUserKey),
                    CreatedAtUtc = DateTime.SpecifyKind(e.CreatedDate, DateTimeKind.Utc)
                })
                .ToList()
        });
    }
}

public sealed class UpsertPrivateScoreEntryRequestValidator : AbstractValidator<UpsertPrivateScoreEntryRequest>
{
    public UpsertPrivateScoreEntryRequestValidator(TimeProvider time)
    {
        RuleFor(r => r.MembershipKey).NotEmpty();
        RuleFor(r => r.Points).NotEqual(0).InclusiveBetween(-1000, 1000);
        RuleFor(r => r.Note).MaximumLength(500);
        RuleFor(r => r).Must(r => r.PrivateScoreCriterionKey.HasValue || !string.IsNullOrWhiteSpace(r.Note))
            .WithMessage("Say what it was for: a criterion, or a note.");
        RuleFor(r => r.OccurredOn).LessThanOrEqualTo(_ => DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(1));
    }
}

public sealed record CreatePrivateScoreEntryCommand(Guid KurinKey, UpsertPrivateScoreEntryRequest Request) : IRequest<ServiceResult<Guid>>;

public sealed class CreatePrivateScoreEntryCommandValidator : AbstractValidator<CreatePrivateScoreEntryCommand>
{
    public CreatePrivateScoreEntryCommandValidator(TimeProvider time)
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.Request).NotNull().SetValidator(new UpsertPrivateScoreEntryRequestValidator(time));
    }
}

/// <summary>What a new and an edited entry share: the youth is here, the criterion is on the list.</summary>
public static class PrivateScoreEntryWriter
{
    public static async Task<ServiceResult<T>?> CheckAsync<T>(IScoreUnitOfWork score, ScoreBook book, UpsertPrivateScoreEntryRequest request, CancellationToken cancellationToken)
    {
        if (!book.Memberships.TryGetValue(request.MembershipKey, out var membership) || membership.Kind != MembershipKind.Youth)
        {
            return ScoreAccess.NotFound<T>("That person is not a youth of this kurin.");
        }

        if (request.PrivateScoreCriterionKey is { } key)
        {
            var criteria = await score.PrivateScoreCriteria.GetForKurinAsync(book.KurinKey, cancellationToken);
            if (criteria.All(c => c.PrivateScoreCriterionKey != key || c.IsArchived))
            {
                return ScoreAccess.NotFound<T>("There is no such criterion on the list.");
            }
        }

        return null;
    }

    public static void Apply(PrivateScoreEntry entry, UpsertPrivateScoreEntryRequest request)
    {
        entry.PrivateScoreCriterionKey = request.PrivateScoreCriterionKey;
        entry.Points = request.Points;
        entry.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        entry.OccurredOn = request.OccurredOn;
    }

    public static object Snapshot(PrivateScoreEntry e) => new
    {
        e.MembershipKey,
        e.PrivateScoreCriterionKey,
        e.Points,
        e.Note,
        e.OccurredOn,
        e.DeletedAtUtc,
        e.DeletedByUserKey
    };
}

public sealed class CreatePrivateScoreEntryCommandHandler : IRequestHandler<CreatePrivateScoreEntryCommand, ServiceResult<Guid>>
{
    private readonly PrivateScoreAccess _access;
    private readonly ScoreBookReader _books;
    private readonly IScoreUnitOfWork _score;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public CreatePrivateScoreEntryCommandHandler(PrivateScoreAccess access, ScoreBookReader books, IScoreUnitOfWork score, ICurrentUserContext currentUser, TimeProvider time)
    {
        _access = access;
        _books = books;
        _score = score;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<Guid>> Handle(CreatePrivateScoreEntryCommand request, CancellationToken cancellationToken)
    {
        if (await _access.RefuseAsync<Guid>(request.KurinKey, ResourceAction.Create, cancellationToken) is { } refused)
        {
            return refused;
        }

        var book = await _books.OpenAsync(request.KurinKey, cancellationToken);
        if (await PrivateScoreEntryWriter.CheckAsync<Guid>(_score, book, request.Request, cancellationToken) is { } problem)
        {
            return problem;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var entry = new PrivateScoreEntry { KurinKey = request.KurinKey, MembershipKey = request.Request.MembershipKey, CreatedByUserKey = _currentUser.UserId, CreatedDate = now, UpdatedDate = now };
        PrivateScoreEntryWriter.Apply(entry, request.Request);
        _score.PrivateScoreEntries.Create(entry, cancellationToken);
        ScoreTrail.Record(_score, request.KurinKey, ScoreTrail.PrivateEntry, entry.PrivateScoreEntryKey, ScoreTrail.Created, PrivateScoreEntryWriter.Snapshot(entry), _currentUser.UserId, now);

        await _score.SaveChangesAsync(cancellationToken);
        return new ServiceResult<Guid>(ResultType.Created, entry.PrivateScoreEntryKey);
    }
}

public sealed record UpdatePrivateScoreEntryCommand(Guid KurinKey, Guid EntryKey, UpsertPrivateScoreEntryRequest Request) : IRequest<ServiceResult<object>>;

public sealed class UpdatePrivateScoreEntryCommandValidator : AbstractValidator<UpdatePrivateScoreEntryCommand>
{
    public UpdatePrivateScoreEntryCommandValidator(TimeProvider time)
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.EntryKey).NotEmpty();
        RuleFor(c => c.Request).NotNull().SetValidator(new UpsertPrivateScoreEntryRequestValidator(time));
    }
}

public sealed class UpdatePrivateScoreEntryCommandHandler : IRequestHandler<UpdatePrivateScoreEntryCommand, ServiceResult<object>>
{
    private readonly PrivateScoreAccess _access;
    private readonly ScoreBookReader _books;
    private readonly IScoreUnitOfWork _score;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public UpdatePrivateScoreEntryCommandHandler(PrivateScoreAccess access, ScoreBookReader books, IScoreUnitOfWork score, ICurrentUserContext currentUser, TimeProvider time)
    {
        _access = access;
        _books = books;
        _score = score;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<object>> Handle(UpdatePrivateScoreEntryCommand request, CancellationToken cancellationToken)
    {
        if (await _access.RefuseAsync<object>(request.KurinKey, ResourceAction.Update, cancellationToken) is { } refused)
        {
            return refused;
        }

        var entry = await _score.PrivateScoreEntries.GetByKeyAsync(request.EntryKey, cancellationToken);
        if (entry is null || entry.KurinKey != request.KurinKey || entry.IsDeleted)
        {
            return ScoreAccess.NotFound<object>("There is no such entry here.");
        }

        if (entry.MembershipKey != request.Request.MembershipKey)
        {
            return ServiceResult<object>.Failure(ResultType.BadRequest, "EntryTargetFixed", "An entry stays with whom it was written about; write a new one instead.");
        }

        var book = await _books.OpenAsync(request.KurinKey, cancellationToken);
        if (await PrivateScoreEntryWriter.CheckAsync<object>(_score, book, request.Request, cancellationToken) is { } problem)
        {
            return problem;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        PrivateScoreEntryWriter.Apply(entry, request.Request);
        entry.UpdatedDate = now;
        _score.PrivateScoreEntries.Update(entry, cancellationToken);
        ScoreTrail.Record(_score, request.KurinKey, ScoreTrail.PrivateEntry, entry.PrivateScoreEntryKey, ScoreTrail.Updated, PrivateScoreEntryWriter.Snapshot(entry), _currentUser.UserId, now);

        await _score.SaveChangesAsync(cancellationToken);
        return new ServiceResult<object>(ResultType.Success);
    }
}

public sealed record DeletePrivateScoreEntryCommand(Guid KurinKey, Guid EntryKey) : IRequest<ServiceResult<object>>;

public sealed class DeletePrivateScoreEntryCommandHandler : IRequestHandler<DeletePrivateScoreEntryCommand, ServiceResult<object>>
{
    private readonly PrivateScoreAccess _access;
    private readonly IScoreUnitOfWork _score;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public DeletePrivateScoreEntryCommandHandler(PrivateScoreAccess access, IScoreUnitOfWork score, ICurrentUserContext currentUser, TimeProvider time)
    {
        _access = access;
        _score = score;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<object>> Handle(DeletePrivateScoreEntryCommand request, CancellationToken cancellationToken)
    {
        if (await _access.RefuseAsync<object>(request.KurinKey, ResourceAction.Delete, cancellationToken) is { } refused)
        {
            return refused;
        }

        var entry = await _score.PrivateScoreEntries.GetByKeyAsync(request.EntryKey, cancellationToken);
        if (entry is null || entry.KurinKey != request.KurinKey || entry.IsDeleted)
        {
            return ScoreAccess.NotFound<object>("There is no such entry here.");
        }

        var now = _time.GetUtcNow().UtcDateTime;
        entry.DeletedAtUtc = now;
        entry.DeletedByUserKey = _currentUser.UserId;
        entry.UpdatedDate = now;
        _score.PrivateScoreEntries.Update(entry, cancellationToken);
        ScoreTrail.Record(_score, request.KurinKey, ScoreTrail.PrivateEntry, entry.PrivateScoreEntryKey, ScoreTrail.Deleted, PrivateScoreEntryWriter.Snapshot(entry), _currentUser.UserId, now);

        await _score.SaveChangesAsync(cancellationToken);
        return new ServiceResult<object>(ResultType.Success);
    }
}

public sealed class UpsertPrivateScoreCriterionRequestValidator : AbstractValidator<UpsertPrivateScoreCriterionRequest>
{
    public UpsertPrivateScoreCriterionRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed record UpsertPrivateScoreCriterionCommand(Guid KurinKey, Guid? CriterionKey, UpsertPrivateScoreCriterionRequest Request) : IRequest<ServiceResult<Guid>>;

public sealed class UpsertPrivateScoreCriterionCommandValidator : AbstractValidator<UpsertPrivateScoreCriterionCommand>
{
    public UpsertPrivateScoreCriterionCommandValidator()
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.Request).NotNull().SetValidator(new UpsertPrivateScoreCriterionRequestValidator());
    }
}

public sealed class UpsertPrivateScoreCriterionCommandHandler : IRequestHandler<UpsertPrivateScoreCriterionCommand, ServiceResult<Guid>>
{
    private readonly PrivateScoreAccess _access;
    private readonly IScoreUnitOfWork _score;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public UpsertPrivateScoreCriterionCommandHandler(PrivateScoreAccess access, IScoreUnitOfWork score, ICurrentUserContext currentUser, TimeProvider time)
    {
        _access = access;
        _score = score;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<Guid>> Handle(UpsertPrivateScoreCriterionCommand request, CancellationToken cancellationToken)
    {
        if (await _access.RefuseAsync<Guid>(request.KurinKey, ResourceAction.Update, cancellationToken) is { } refused)
        {
            return refused;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        PrivateScoreCriterion criterion;
        if (request.CriterionKey is { } key)
        {
            var found = await _score.PrivateScoreCriteria.GetByKeyAsync(key, cancellationToken);
            if (found is null || found.KurinKey != request.KurinKey)
            {
                return ScoreAccess.NotFound<Guid>("There is no such criterion.");
            }

            criterion = found;
        }
        else
        {
            criterion = new PrivateScoreCriterion { KurinKey = request.KurinKey, CreatedDate = now };
            _score.PrivateScoreCriteria.Create(criterion, cancellationToken);
        }

        criterion.Name = request.Request.Name.Trim();
        criterion.IsArchived = request.Request.IsArchived;
        criterion.UpdatedDate = now;
        ScoreTrail.Record(_score, request.KurinKey, ScoreTrail.PrivateCriterion, criterion.PrivateScoreCriterionKey,
            request.CriterionKey is null ? ScoreTrail.Created : ScoreTrail.Updated, new { criterion.Name, criterion.IsArchived }, _currentUser.UserId, now);

        await _score.SaveChangesAsync(cancellationToken);
        return new ServiceResult<Guid>(request.CriterionKey is null ? ResultType.Created : ResultType.Success, criterion.PrivateScoreCriterionKey);
    }
}
