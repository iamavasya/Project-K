using AutoMapper;
using FluentValidation;
using MediatR;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Catalog;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.SeniorRecord;

/// <summary>
/// Adds a line to the caller's own книжечка, or changes one (<see cref="MemberSeniorRecordKey"/> set).
/// Nobody confirms it and nobody else may write it: in a senior kurin each person keeps their own.
/// </summary>
public sealed class UpsertMySeniorRecordCommand : IRequest<ServiceResult<MemberSeniorRecordDto>>
{
    public Guid? MemberSeniorRecordKey { get; set; }
    public string CatalogId { get; set; } = string.Empty;
    public string? Number { get; set; }
    public DateOnly? AchievedOn { get; set; }
    public string? Note { get; set; }
}

/// <summary>Takes a line out of the caller's own книжечка.</summary>
public sealed record DeleteMySeniorRecordCommand(Guid MemberSeniorRecordKey) : IRequest<ServiceResult<bool>>;

public sealed class UpsertMySeniorRecordCommandValidator : AbstractValidator<UpsertMySeniorRecordCommand>
{
    public UpsertMySeniorRecordCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.CatalogId).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Number).MaximumLength(MemberSeniorRecord.NumberMaxLength);
        RuleFor(command => command.Note).MaximumLength(MemberSeniorRecord.NoteMaxLength);

        // A day of slack, as for відзначення: «сьогодні» in Kyiv is tomorrow in UTC for a few hours.
        RuleFor(command => command.AchievedOn)
            .Must(date => date is null || date.Value <= DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(1)))
            .WithMessage("Nothing in the книжечка can be achieved in the future.");
    }
}

public sealed class UpsertMySeniorRecordCommandHandler : IRequestHandler<UpsertMySeniorRecordCommand, ServiceResult<MemberSeniorRecordDto>>
{
    private readonly IMemberUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ISeniorRecordsCatalog _catalog;
    private readonly IMapper _mapper;

    public UpsertMySeniorRecordCommandHandler(
        IMemberUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ISeniorRecordsCatalog catalog,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _catalog = catalog;
        _mapper = mapper;
    }

    public async Task<ServiceResult<MemberSeniorRecordDto>> Handle(UpsertMySeniorRecordCommand request, CancellationToken cancellationToken)
    {
        var memberKey = await SeniorRecordOwner.MemberKeyAsync(_unitOfWork, _currentUser, cancellationToken);
        if (memberKey is null)
        {
            return new ServiceResult<MemberSeniorRecordDto>(ResultType.Forbidden);
        }

        var item = _catalog.GetById(request.CatalogId.Trim());
        if (item is null)
        {
            return ServiceResult<MemberSeniorRecordDto>.Failure(
                ResultType.BadRequest, "UnknownCatalogItem", "The catalogue has no such ступінь, відзначення or вишкіл.");
        }

        var mine = await _unitOfWork.MemberSeniorRecords.GetByMemberKeyAsync(memberKey.Value, cancellationToken);

        // A ступінь is taken once; відзначення and вишколи may come again (a second грамота, another табір).
        if (item.Kind == SeniorRecordKind.Degree
            && mine.Any(record => record.CatalogId == item.Id && record.MemberSeniorRecordKey != request.MemberSeniorRecordKey))
        {
            return ServiceResult<MemberSeniorRecordDto>.Failure(
                ResultType.Conflict, "DegreeAlreadyRecorded", "This ступінь is already in the книжечка.");
        }

        MemberSeniorRecord record;
        if (request.MemberSeniorRecordKey is { } key)
        {
            var existing = await _unitOfWork.MemberSeniorRecords.GetByKeyAsync(key, cancellationToken);
            // Someone else's line answers as missing: the caller has no business knowing it exists.
            if (existing is null || existing.MemberKey != memberKey.Value)
            {
                return new ServiceResult<MemberSeniorRecordDto>(ResultType.NotFound);
            }

            record = existing;
            record.UpdatedDate = DateTime.UtcNow;
        }
        else
        {
            record = new MemberSeniorRecord { MemberKey = memberKey.Value };
            _unitOfWork.MemberSeniorRecords.Create(record, cancellationToken);
        }

        record.CatalogId = item.Id;
        record.Number = Blank(request.Number);
        record.AchievedOn = request.AchievedOn;
        record.Note = Blank(request.Note);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ServiceResult<MemberSeniorRecordDto>(
            request.MemberSeniorRecordKey is null ? ResultType.Created : ResultType.Success,
            _mapper.Map<MemberSeniorRecordDto>(record));
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class DeleteMySeniorRecordCommandHandler : IRequestHandler<DeleteMySeniorRecordCommand, ServiceResult<bool>>
{
    private readonly IMemberUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public DeleteMySeniorRecordCommandHandler(IMemberUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<bool>> Handle(DeleteMySeniorRecordCommand request, CancellationToken cancellationToken)
    {
        var memberKey = await SeniorRecordOwner.MemberKeyAsync(_unitOfWork, _currentUser, cancellationToken);
        if (memberKey is null)
        {
            return new ServiceResult<bool>(ResultType.Forbidden);
        }

        var record = await _unitOfWork.MemberSeniorRecords.GetByKeyAsync(request.MemberSeniorRecordKey, cancellationToken);
        if (record is null || record.MemberKey != memberKey.Value)
        {
            return new ServiceResult<bool>(ResultType.NotFound);
        }

        _unitOfWork.MemberSeniorRecords.Delete(record, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ServiceResult<bool>(ResultType.Success, true);
    }
}

/// <summary>Whose книжечка the caller writes: the person behind their own account, and nobody else's.</summary>
internal static class SeniorRecordOwner
{
    public static async Task<Guid?> MemberKeyAsync(IMemberUnitOfWork unitOfWork, ICurrentUserContext currentUser, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userKey)
        {
            return null;
        }

        return (await unitOfWork.Members.GetSummaryByUserKeyAsync(userKey, cancellationToken))?.MemberKey;
    }
}
