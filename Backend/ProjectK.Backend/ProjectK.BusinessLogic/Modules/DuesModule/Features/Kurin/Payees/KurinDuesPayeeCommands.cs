using FluentValidation;
using MediatR;
using ProjectK.Common.Entities.DuesModule;
using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.DuesModule.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Features.Kurin.Payees;

/// <summary>Names a new payee of the kurin — someone its статті can send вкладка to.</summary>
public sealed record CreateKurinDuesPayeeCommand(Guid KurinKey, UpsertKurinDuesPayeeRequest Request) : IRequest<ServiceResult<Guid>>;

/// <summary>Renames a payee, or archives it or brings it back. It is never deleted: статті and transfers point at it.</summary>
public sealed record UpdateKurinDuesPayeeCommand(Guid KurinKey, Guid PayeeKey, UpsertKurinDuesPayeeRequest Request) : IRequest<ServiceResult<object>>;

public sealed class UpsertKurinDuesPayeeRequestValidator : AbstractValidator<UpsertKurinDuesPayeeRequest>
{
    public UpsertKurinDuesPayeeRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().Must(n => n?.Trim().Length > 0).MaximumLength(KurinDuesPayee.MaxNameLength);
    }
}

public sealed class CreateKurinDuesPayeeCommandValidator : AbstractValidator<CreateKurinDuesPayeeCommand>
{
    public CreateKurinDuesPayeeCommandValidator()
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.Request).NotNull().SetValidator(new UpsertKurinDuesPayeeRequestValidator());
        RuleFor(c => c.Request.IsArchived).Equal(false).WithMessage("A new payee is not archived.");
    }
}

public sealed class UpdateKurinDuesPayeeCommandValidator : AbstractValidator<UpdateKurinDuesPayeeCommand>
{
    public UpdateKurinDuesPayeeCommandValidator()
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.PayeeKey).NotEmpty();
        RuleFor(c => c.Request).NotNull().SetValidator(new UpsertKurinDuesPayeeRequestValidator());
    }
}

public sealed class CreateKurinDuesPayeeCommandHandler : IRequestHandler<CreateKurinDuesPayeeCommand, ServiceResult<Guid>>
{
    private readonly IDuesUnitOfWork _dues;
    private readonly ICurrentUserContext _currentUser;

    public CreateKurinDuesPayeeCommandHandler(IDuesUnitOfWork dues, ICurrentUserContext currentUser)
    {
        _dues = dues;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<Guid>> Handle(CreateKurinDuesPayeeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.KurinKey != request.KurinKey)
        {
            return new ServiceResult<Guid>(ResultType.Forbidden);
        }

        var name = request.Request.Name.Trim();
        var payees = await _dues.KurinDuesPayees.GetForKurinAsync(request.KurinKey, cancellationToken);
        if (KurinDuesPayees.NameTaken(payees, name, except: null))
        {
            return KurinDuesPayees.Taken<Guid>();
        }

        var payee = new KurinDuesPayee { KurinKey = request.KurinKey, Name = name };
        _dues.KurinDuesPayees.Create(payee, cancellationToken);
        await _dues.SaveChangesAsync(cancellationToken);
        return new ServiceResult<Guid>(ResultType.Created, payee.KurinDuesPayeeKey);
    }
}

public sealed class UpdateKurinDuesPayeeCommandHandler : IRequestHandler<UpdateKurinDuesPayeeCommand, ServiceResult<object>>
{
    private readonly IDuesUnitOfWork _dues;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public UpdateKurinDuesPayeeCommandHandler(IDuesUnitOfWork dues, ICurrentUserContext currentUser, TimeProvider time)
    {
        _dues = dues;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<object>> Handle(UpdateKurinDuesPayeeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.KurinKey != request.KurinKey)
        {
            return new ServiceResult<object>(ResultType.Forbidden);
        }

        var payees = await _dues.KurinDuesPayees.GetForKurinAsync(request.KurinKey, cancellationToken);
        var payee = payees.FirstOrDefault(p => p.KurinDuesPayeeKey == request.PayeeKey);
        if (payee is null)
        {
            return ServiceResult<object>.Failure(ResultType.NotFound, "PayeeNotFound", "The kurin has no such payee.");
        }

        var name = request.Request.Name.Trim();
        if (KurinDuesPayees.NameTaken(payees, name, except: payee.KurinDuesPayeeKey))
        {
            return KurinDuesPayees.Taken<object>();
        }

        payee.Name = name;
        if (request.Request.IsArchived != payee.IsArchived)
        {
            payee.ArchivedAtUtc = request.Request.IsArchived ? _time.GetUtcNow().UtcDateTime : null;
        }

        payee.UpdatedDate = _time.GetUtcNow().UtcDateTime;
        await _dues.SaveChangesAsync(cancellationToken);
        return new ServiceResult<object>(ResultType.Success);
    }
}

internal static class KurinDuesPayees
{
    public static bool NameTaken(IEnumerable<KurinDuesPayee> payees, string name, Guid? except) =>
        payees.Any(p => p.KurinDuesPayeeKey != except && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    public static ServiceResult<T> Taken<T>() =>
        ServiceResult<T>.Failure(ResultType.Conflict, "PayeeNameTaken", "The kurin already has a payee with that name.");
}
