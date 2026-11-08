using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Exceptions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Events;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MembershipRequest.Invitations;

/// <summary>
/// The kurins waiting on the signed-in person's own answer: requests across the youth and senior
/// lines that no kurin answers for them (<see cref="MembershipAskRouting"/>).
/// </summary>
public sealed record GetMyInvitationsQuery : IRequest<ServiceResult<IReadOnlyList<MembershipInvitationDto>>>;

/// <summary>One invitation as the person reads it: who asks, and how they would stand there.</summary>
public sealed record MembershipInvitationDto(
    Guid MembershipRequestKey,
    MembershipRequestKurin Kurin,
    MembershipKind MembershipKind,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);

public sealed class GetMyInvitationsQueryHandler
    : IRequestHandler<GetMyInvitationsQuery, ServiceResult<IReadOnlyList<MembershipInvitationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public GetMyInvitationsQueryHandler(IUnitOfWork unitOfWork, IMemberDirectory members, ICurrentUserContext currentUser, TimeProvider time)
    {
        _unitOfWork = unitOfWork;
        _members = members;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<IReadOnlyList<MembershipInvitationDto>>> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
    {
        var me = _currentUser.UserId is { } userKey ? await _members.FindByAccountAsync(userKey, cancellationToken) : null;
        if (me is null)
        {
            return new ServiceResult<IReadOnlyList<MembershipInvitationDto>>(ResultType.Success, []);
        }

        var open = await _unitOfWork.MembershipRequests.GetOpenForPersonAsync(me.MemberKey, _time.GetUtcNow().UtcDateTime, cancellationToken);
        var result = new List<MembershipInvitationDto>(open.Count);
        foreach (var invitation in open)
        {
            var kurin = await _unitOfWork.Kurins.GetByKeyAsync(invitation.ToKurinKey, cancellationToken);
            result.Add(new MembershipInvitationDto(
                invitation.MembershipRequestKey,
                MembershipRequestKurin.Of(invitation.ToKurinKey, kurin),
                invitation.MembershipKind,
                invitation.CreatedAtUtc,
                invitation.ExpiresAtUtc));
        }

        return new ServiceResult<IReadOnlyList<MembershipInvitationDto>>(ResultType.Success, result);
    }
}

/// <summary>
/// The person accepts or declines a kurin's invitation. Accepting only adds a membership: nothing
/// they hold elsewhere closes.
/// </summary>
public sealed record AnswerMyInvitationCommand(Guid MembershipRequestKey, bool Accept) : IRequest<ServiceResult<object>>;

public sealed class AnswerMyInvitationCommandHandler : IRequestHandler<AnswerMyInvitationCommand, ServiceResult<object>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberDirectory _members;
    private readonly MembershipMoves _moves;
    private readonly IDomainEventPublisher _events;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAccessChangeSignal _accessChanges;
    private readonly TimeProvider _time;

    public AnswerMyInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMemberDirectory members,
        MembershipMoves moves,
        IDomainEventPublisher events,
        ICurrentUserContext currentUser,
        IAccessChangeSignal accessChanges,
        TimeProvider time)
    {
        _unitOfWork = unitOfWork;
        _members = members;
        _moves = moves;
        _events = events;
        _currentUser = currentUser;
        _accessChanges = accessChanges;
        _time = time;
    }

    public async Task<ServiceResult<object>> Handle(AnswerMyInvitationCommand request, CancellationToken cancellationToken)
    {
        var me = _currentUser.UserId is { } userKey ? await _members.FindByAccountAsync(userKey, cancellationToken) : null;
        var asked = await _unitOfWork.MembershipRequests.GetAsync(request.MembershipRequestKey, cancellationToken);
        if (me is null || asked is null || asked.FromKurinKey is not null || asked.MemberKey != me.MemberKey)
        {
            return ServiceResult<object>.Failure(ResultType.NotFound, "RequestNotFound", "No such invitation for you.");
        }

        var now = _time.GetUtcNow().UtcDateTime;
        if (!asked.IsOpen(now))
        {
            return ServiceResult<object>.Failure(ResultType.Conflict, "RequestClosed", "The invitation was already answered, withdrawn or has lapsed.");
        }

        if (request.Accept)
        {
            asked.Close(MembershipRequestStatus.Approved, now, _currentUser.UserId);
            if (await _unitOfWork.Memberships.GetActiveAsync(asked.MemberKey, asked.ToKurinKey, cancellationToken) is null)
            {
                await _moves.OpenAsync(me, asked.ToKurinKey, asked.MembershipKind, cancellationToken);
            }
        }
        else
        {
            asked.Close(MembershipRequestStatus.Declined, now, _currentUser.UserId);
        }

        try
        {
            await _events.PublishAsync(
                new MembershipRequestDecided(
                    asked.MembershipRequestKey, asked.Kind, asked.MemberKey, me.UserKey, me.FullName,
                    null, asked.ToKurinKey, request.Accept, _currentUser.UserId),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (StaleRowException)
        {
            return ServiceResult<object>.Failure(ResultType.Conflict, "RequestClosed", "The invitation was already answered, withdrawn or has lapsed.");
        }

        if (request.Accept && me.UserKey is { } account)
        {
            _accessChanges.MarkUser(account);
        }

        return new ServiceResult<object>(ResultType.Success);
    }
}
