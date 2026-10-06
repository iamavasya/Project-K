using FluentValidation;
using MediatR;
using ProjectK.Common.Entities.DuesModule;
using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.DuesModule.Requests;
using ProjectK.Common.Models.Dues;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Features.Kurin.SetRate;

/// <summary>
/// Sets the станиця and kurin parts of the вкладка from a quarter on, for every гурток of the kurin.
/// A rate already set from that quarter is replaced.
/// </summary>
public sealed record SetKurinDuesRateCommand(Guid KurinKey, SetKurinDuesRateRequest Request) : IRequest<ServiceResult<object>>;

public sealed class SetKurinDuesRateCommandValidator : AbstractValidator<SetKurinDuesRateCommand>
{
    public SetKurinDuesRateCommandValidator()
    {
        RuleFor(c => c.KurinKey).NotEmpty();
        RuleFor(c => c.Request.FromQuarter.Number).InclusiveBetween(1, 4);
        RuleFor(c => c.Request.FromQuarter.Year).InclusiveBetween(2000, 2100);
        RuleFor(c => c.Request.StanytsiaFull).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(c => c.Request.StanytsiaReduced).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(c => c.Request.StanytsiaReduced).LessThanOrEqualTo(c => c.Request.StanytsiaFull)
            .WithMessage("The пільгова rate cannot be above the full one.");
        RuleFor(c => c.Request.KurinShare).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}

public sealed class SetKurinDuesRateCommandHandler : IRequestHandler<SetKurinDuesRateCommand, ServiceResult<object>>
{
    private readonly IDuesUnitOfWork _dues;
    private readonly ICurrentUserContext _currentUser;

    public SetKurinDuesRateCommandHandler(IDuesUnitOfWork dues, ICurrentUserContext currentUser)
    {
        _dues = dues;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<object>> Handle(SetKurinDuesRateCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.KurinKey != request.KurinKey)
        {
            return new ServiceResult<object>(ResultType.Forbidden);
        }

        var from = new DuesQuarter(request.Request.FromQuarter.Year, request.Request.FromQuarter.Number).Index;
        var existing = (await _dues.KurinDuesRates.GetForKurinAsync(request.KurinKey, cancellationToken))
            .FirstOrDefault(r => r.FromQuarter == from);

        if (existing is null)
        {
            _dues.KurinDuesRates.Create(new KurinDuesRate
            {
                KurinKey = request.KurinKey,
                FromQuarter = from,
                StanytsiaFull = request.Request.StanytsiaFull,
                StanytsiaReduced = request.Request.StanytsiaReduced,
                KurinShare = request.Request.KurinShare,
                SetByUserKey = _currentUser.UserId
            });
        }
        else
        {
            existing.StanytsiaFull = request.Request.StanytsiaFull;
            existing.StanytsiaReduced = request.Request.StanytsiaReduced;
            existing.KurinShare = request.Request.KurinShare;
            existing.SetByUserKey = _currentUser.UserId;
            existing.UpdatedDate = DateTime.UtcNow;
            _dues.KurinDuesRates.Update(existing, cancellationToken);
        }

        await _dues.SaveChangesAsync(cancellationToken);
        return new ServiceResult<object>(ResultType.Success);
    }
}
