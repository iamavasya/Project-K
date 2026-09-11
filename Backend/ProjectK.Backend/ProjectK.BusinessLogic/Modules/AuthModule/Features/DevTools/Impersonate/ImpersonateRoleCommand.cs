using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.DevTools.Impersonate;

/// <summary>The seats the dev role switcher can put an administrator into.</summary>
public enum DevRole
{
    Zvyazkovyi,
    Vykhovnyk,
    Kurinnyi,
    Skarbnyk,
    /// <summary>A member of the kurin with no kurin-wide or КВ office and no гурток to mentor; a гуртковий office is fine.</summary>
    Member
}

/// <summary>
/// Signs the administrator in as somebody who holds <paramref name="Role"/> in the kurin, with a
/// ticket that brings them back. A testing tool: it exists only on the local tiers, where the
/// controller that sends it is part of the application at all.
/// </summary>
public record ImpersonateRoleCommand(DevRole Role, Guid? KurinKey) : IRequest<ServiceResult<DevImpersonationResponse>>;

public class ImpersonateRoleCommandHandler : IRequestHandler<ImpersonateRoleCommand, ServiceResult<DevImpersonationResponse>>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberDirectory _members;
    private readonly IMembershipDirectory _memberships;
    private readonly ILoginResponseFactory _loginResponseFactory;
    private readonly IJwtService _jwtService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IActivityLogger _activityLogger;

    public ImpersonateRoleCommandHandler(
        UserManager<AppUser> userManager,
        IUnitOfWork unitOfWork,
        IMemberDirectory members,
        IMembershipDirectory memberships,
        ILoginResponseFactory loginResponseFactory,
        IJwtService jwtService,
        ICurrentUserContext currentUser,
        IActivityLogger activityLogger)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _members = members;
        _memberships = memberships;
        _loginResponseFactory = loginResponseFactory;
        _jwtService = jwtService;
        _currentUser = currentUser;
        _activityLogger = activityLogger;
    }

    public async Task<ServiceResult<DevImpersonationResponse>> Handle(ImpersonateRoleCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } actorKey || !_currentUser.IsAdmin())
        {
            return ServiceResult<DevImpersonationResponse>.Failure(ResultType.Forbidden, "AdminOnly", "Only an administrator can step into another role.");
        }

        // The kurin the admin is looking at, else the demo kurin: the tool is for testing a
        // feature quickly, and "the kurin on screen" is what a tester means.
        var kurinKey = request.KurinKey ?? _currentUser.KurinKey;
        if (kurinKey is null)
        {
            var demoKurin = await _unitOfWork.Kurins.GetByNumberAsync(1, cancellationToken);
            kurinKey = demoKurin?.KurinKey;
        }

        if (kurinKey is null)
        {
            return ServiceResult<DevImpersonationResponse>.Failure(ResultType.NotFound, "NoKurin", "There is no kurin to step into.");
        }

        var target = await FindAccountHoldingAsync(request.Role, kurinKey.Value, actorKey, cancellationToken);
        if (target is null)
        {
            return ServiceResult<DevImpersonationResponse>.Failure(
                ResultType.NotFound,
                "NoAccountForRole",
                $"Nobody in this kurin holds {request.Role} with an account to sign in as.");
        }

        // Land them in this kurin even when the person belongs to several.
        target.ActiveKurinKey = kurinKey;
        await _userManager.UpdateAsync(target);

        var login = await _loginResponseFactory.CreateAsync(target, cancellationToken);
        var ticket = _jwtService.GenerateDevReturnTicket(actorKey);

        _activityLogger.LogAudit(
            action: "Dev.Impersonate",
            actorUserId: actorKey,
            targetUserId: target.Id,
            reason: $"Dev role switcher: {request.Role} in kurin {kurinKey}.");

        return new ServiceResult<DevImpersonationResponse>(
            ResultType.Success,
            new DevImpersonationResponse(login, ticket, request.Role.ToString(), kurinKey.Value));
    }

    private async Task<AppUser?> FindAccountHoldingAsync(DevRole role, Guid kurinKey, Guid actorKey, CancellationToken cancellationToken)
    {
        if (role == DevRole.Member)
        {
            return await FindPlainMemberAsync(kurinKey, actorKey, cancellationToken);
        }

        var office = role switch
        {
            DevRole.Zvyazkovyi => LeadershipRole.Zvyazkovyi,
            DevRole.Vykhovnyk => LeadershipRole.Vykhovnyk,
            DevRole.Kurinnyi => LeadershipRole.Kurinnuy,
            DevRole.Skarbnyk => LeadershipRole.Skarbnyk,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };

        var holders = await _unitOfWork.Leaderships.GetActiveOfficeMemberKeysAsync([office], kurinKey, cancellationToken: cancellationToken);
        foreach (var memberKey in holders)
        {
            var accountKey = await _members.FindAccountKeyAsync(memberKey, cancellationToken);
            if (accountKey is null || accountKey == actorKey)
            {
                continue;
            }

            var user = await _userManager.FindByIdAsync(accountKey.Value.ToString());
            if (user is not null && user.CanSignIn())
            {
                return user;
            }
        }

        return null;
    }

    /// <summary>
    /// Somebody in the kurin with an account, no admin role, and nothing that grants rights beyond
    /// their own гурток: no kurin-wide or КВ office, no mentorship. A гуртковий or писар of a гурток
    /// still counts as a plain youth, and in demo data nearly everyone holds one of those.
    /// </summary>
    private async Task<AppUser?> FindPlainMemberAsync(Guid kurinKey, Guid actorKey, CancellationToken cancellationToken)
    {
        var accounts = await _memberships.GetAccountKeysInKurinAsync(kurinKey, cancellationToken);
        foreach (var accountKey in accounts)
        {
            if (accountKey == actorKey)
            {
                continue;
            }

            var offices = await _unitOfWork.Leaderships.GetActiveOfficesForAccountInKurinAsync(accountKey, kurinKey, cancellationToken);
            if (offices.Any(office => office.Type != LeadershipType.Group))
            {
                continue;
            }

            var user = await _userManager.FindByIdAsync(accountKey.ToString());
            if (user is null || !user.CanSignIn() || await _userManager.IsInRoleAsync(user, SystemRole.Admin))
            {
                continue;
            }

            return user;
        }

        return null;
    }
}
