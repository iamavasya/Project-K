using FluentValidation;
using MediatR;
using ProjectK.BusinessLogic.Modules.DuesModule.Services;
using ProjectK.Common.Entities.DuesModule;
using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.DuesModule.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Features.Kurin.Entry;

// The kurin's own operations: what the course of the гурток's ones is, minus the person — the
// kurin's box holds expenses, income, exchanges and what goes to the станиця.

public sealed record CreateKurinDuesEntryCommand(Guid KurinKey, UpsertDuesEntryRequest Request) : IRequest<ServiceResult<Guid>>;

public sealed class CreateKurinDuesEntryCommandValidator : AbstractValidator<CreateKurinDuesEntryCommand>
{
    public CreateKurinDuesEntryCommandValidator(TimeProvider time)
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.Request).NotNull().SetValidator(new UpsertKurinDuesEntryRequestValidator(time));
    }
}

public sealed class CreateKurinDuesEntryCommandHandler : IRequestHandler<CreateKurinDuesEntryCommand, ServiceResult<Guid>>
{
    private readonly KurinDuesAccess _access;
    private readonly DuesEntryWriter _writer;
    private readonly IDuesUnitOfWork _dues;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public CreateKurinDuesEntryCommandHandler(KurinDuesAccess access, DuesEntryWriter writer, IDuesUnitOfWork dues, ICurrentUserContext currentUser, TimeProvider time)
    {
        _access = access;
        _writer = writer;
        _dues = dues;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<Guid>> Handle(CreateKurinDuesEntryCommand request, CancellationToken cancellationToken)
    {
        if (_access.Refuse<Guid>(request.KurinKey) is { } refused)
        {
            return refused;
        }

        if (await _writer.CheckForKurinAsync<Guid>(request.KurinKey, request.Request, cancellationToken) is { } problem)
        {
            return problem;
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var entry = new DuesEntry { KurinKey = request.KurinKey, GroupKey = null, CreatedByUserKey = _currentUser.UserId, CreatedDate = now };
        DuesEntryWriter.Apply(entry, request.Request);
        DuesEntryTrail.Record(entry, DuesEntryTrail.Created, _currentUser.UserId, now);

        _dues.DuesEntries.Create(entry, cancellationToken);
        await _dues.SaveChangesAsync(cancellationToken);

        return new ServiceResult<Guid>(ResultType.Created, entry.DuesEntryKey);
    }
}

public sealed record UpdateKurinDuesEntryCommand(Guid KurinKey, Guid EntryKey, UpsertDuesEntryRequest Request) : IRequest<ServiceResult<object>>;

public sealed class UpdateKurinDuesEntryCommandValidator : AbstractValidator<UpdateKurinDuesEntryCommand>
{
    public UpdateKurinDuesEntryCommandValidator(TimeProvider time)
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.EntryKey).NotEmpty();
        RuleFor(c => c.Request).NotNull().SetValidator(new UpsertKurinDuesEntryRequestValidator(time));
    }
}

public sealed class UpdateKurinDuesEntryCommandHandler : IRequestHandler<UpdateKurinDuesEntryCommand, ServiceResult<object>>
{
    private readonly KurinDuesAccess _access;
    private readonly DuesEntryWriter _writer;
    private readonly IDuesUnitOfWork _dues;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public UpdateKurinDuesEntryCommandHandler(KurinDuesAccess access, DuesEntryWriter writer, IDuesUnitOfWork dues, ICurrentUserContext currentUser, TimeProvider time)
    {
        _access = access;
        _writer = writer;
        _dues = dues;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<object>> Handle(UpdateKurinDuesEntryCommand request, CancellationToken cancellationToken)
    {
        if (_access.Refuse<object>(request.KurinKey) is { } refused)
        {
            return refused;
        }

        var entry = await _access.OwnEntryAsync(request.KurinKey, request.EntryKey, cancellationToken);
        if (entry is null)
        {
            return KurinDuesAccess.NotFound<object>();
        }

        if (entry.IsVerified)
        {
            return KurinDuesAccess.Locked<object>();
        }

        if (await _writer.CheckForKurinAsync<object>(request.KurinKey, request.Request, cancellationToken) is { } problem)
        {
            return problem;
        }

        DuesEntryWriter.Apply(entry, request.Request);
        DuesEntryTrail.Record(entry, DuesEntryTrail.Updated, _currentUser.UserId, _time.GetUtcNow().UtcDateTime);
        await _dues.SaveChangesAsync(cancellationToken);

        return new ServiceResult<object>(ResultType.Success);
    }
}

public sealed record DeleteKurinDuesEntryCommand(Guid KurinKey, Guid EntryKey) : IRequest<ServiceResult<object>>;

public sealed class DeleteKurinDuesEntryCommandHandler : IRequestHandler<DeleteKurinDuesEntryCommand, ServiceResult<object>>
{
    private readonly KurinDuesAccess _access;
    private readonly IDuesUnitOfWork _dues;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public DeleteKurinDuesEntryCommandHandler(KurinDuesAccess access, IDuesUnitOfWork dues, ICurrentUserContext currentUser, TimeProvider time)
    {
        _access = access;
        _dues = dues;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<object>> Handle(DeleteKurinDuesEntryCommand request, CancellationToken cancellationToken)
    {
        if (_access.Refuse<object>(request.KurinKey) is { } refused)
        {
            return refused;
        }

        var entry = await _access.OwnEntryAsync(request.KurinKey, request.EntryKey, cancellationToken);
        if (entry is null)
        {
            return KurinDuesAccess.NotFound<object>();
        }

        if (entry.IsVerified)
        {
            return KurinDuesAccess.Locked<object>();
        }

        var now = _time.GetUtcNow().UtcDateTime;
        entry.DeletedAtUtc = now;
        entry.DeletedByUserKey = _currentUser.UserId;
        DuesEntryTrail.Record(entry, DuesEntryTrail.Deleted, _currentUser.UserId, now);
        await _dues.SaveChangesAsync(cancellationToken);

        return new ServiceResult<object>(ResultType.Success);
    }
}

/// <summary>The Звʼязковий's mark on a kurin operation; setting it locks the operation.</summary>
public sealed record SetKurinDuesEntryVerifiedCommand(Guid KurinKey, Guid EntryKey, bool IsVerified) : IRequest<ServiceResult<object>>;

public sealed class SetKurinDuesEntryVerifiedCommandHandler : IRequestHandler<SetKurinDuesEntryVerifiedCommand, ServiceResult<object>>
{
    private readonly KurinDuesAccess _access;
    private readonly IDuesUnitOfWork _dues;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public SetKurinDuesEntryVerifiedCommandHandler(KurinDuesAccess access, IDuesUnitOfWork dues, ICurrentUserContext currentUser, TimeProvider time)
    {
        _access = access;
        _dues = dues;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<object>> Handle(SetKurinDuesEntryVerifiedCommand request, CancellationToken cancellationToken)
    {
        if (_access.Refuse<object>(request.KurinKey) is { } refused)
        {
            return refused;
        }

        var entry = await _access.OwnEntryAsync(request.KurinKey, request.EntryKey, cancellationToken);
        if (entry is null)
        {
            return KurinDuesAccess.NotFound<object>();
        }

        if (entry.IsVerified == request.IsVerified)
        {
            return new ServiceResult<object>(ResultType.Success);
        }

        var now = _time.GetUtcNow().UtcDateTime;
        entry.VerifiedAtUtc = request.IsVerified ? now : null;
        entry.VerifiedByUserKey = request.IsVerified ? _currentUser.UserId : null;
        DuesEntryTrail.Record(entry, request.IsVerified ? DuesEntryTrail.Verified : DuesEntryTrail.Unverified, _currentUser.UserId, now);
        await _dues.SaveChangesAsync(cancellationToken);

        return new ServiceResult<object>(ResultType.Success);
    }
}
