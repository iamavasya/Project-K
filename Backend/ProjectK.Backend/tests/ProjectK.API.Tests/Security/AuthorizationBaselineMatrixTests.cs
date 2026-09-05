using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using ProjectK.API.Controllers.AuthModule;
using ProjectK.API.Controllers.KurinModule;
using ProjectK.API.Controllers.ProbesAndBadgesModule;
using ProjectK.API.Controllers.UsersModule;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Categories;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Create;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Update;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Create;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.Common.Models.Enums;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.API.Models.Requests;
using ProjectK.Common.Models.Dtos.KurinModule.Requests;
using ProjectK.Common.Models.Dtos.ProbesAndBadgesModule.Requests;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.API.Authorization;
using ProjectK.API.Controllers.InfrastructureModule;
using ProjectK.API.Controllers.TestModule;

namespace ProjectK.API.Tests.Security;

public class AuthorizationBaselineMatrixTests
{
    [Theory]
    [MemberData(nameof(PolicyEndpoints))]
    public void Endpoint_ShouldHaveExpectedPolicy(MethodInfo action, string policy)
    {
        var allowAnonymous = action.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.Null(allowAnonymous);

        var authorize = action.GetCustomAttribute<AuthorizeAttribute>()
            ?? action.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(policy, authorize!.Policy);
    }

    [Theory]
    [MemberData(nameof(AllowAnonymousEndpoints))]
    public void Endpoint_ShouldBeExplicitAllowAnonymous(MethodInfo action)
    {
        var allowAnonymous = action.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnonymous);
    }

    /// <summary>
    /// The matrix only guards what it lists, so a whole controller can slip in unchecked — that is how
    /// the agenda endpoints went unlisted. This fails until every action is accounted for.
    /// </summary>
    [Fact]
    public void EveryControllerAction_ShouldBeCoveredByTheMatrix()
    {
        var covered = PolicyEndpoints()
            .Select(row => (MethodInfo)row[0])
            .Concat(AllowAnonymousEndpoints().Select(row => (MethodInfo)row[0]))
            .ToHashSet();

        var uncovered = typeof(AgendaController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Where(method => !covered.Contains(method))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .OrderBy(name => name)
            .ToList();

        var unexpected = uncovered.Except(KnownUnlistedEndpoints).ToList();
        Assert.True(
            unexpected.Count == 0,
            $"These endpoints are not in the authorization baseline: {string.Join(", ", unexpected)}. "
            + "Add a row for each, or extend KnownUnlistedEndpoints if the gap is deliberate.");

        var closed = KnownUnlistedEndpoints.Except(uncovered).ToList();
        Assert.True(
            closed.Count == 0,
            $"These endpoints are now listed and can leave KnownUnlistedEndpoints: {string.Join(", ", closed)}.");
    }

    /// <summary>
    /// Nothing is unlisted any more. Every controller action has a row below, reviewed one by one in
    /// 0.19.0 — the set is kept so a newly added endpoint has somewhere to be pinned deliberately,
    /// rather than being added here by reflex.
    /// </summary>
    private static readonly IReadOnlySet<string> KnownUnlistedEndpoints = new HashSet<string>();

    public static IEnumerable<object[]> PolicyEndpoints()
    {
        yield return Row<Action<AuthController, RegisterUserRequest>>(nameof(AuthController.RegisterKurin), "RequireAdmin");
        yield return Row<Action<AuthController, RegisterUserRequest>>(nameof(AuthController.Register), AuthorizationPolicies.RequireKurinManagement);
        yield return Row<Action<AuthController>>(nameof(AuthController.Logout), "RequireUser");
        yield return Row<Action<AuthController, CheckEntityAccessRequest>>(nameof(AuthController.CheckAccess), "RequireUser");
        yield return Row<Action<AuthController>>(nameof(AuthController.GetMfaSetup), "RequireUser");
        yield return Row<Action<AuthController, MfaVerifyRequestDto>>(nameof(AuthController.EnableMfa), "RequireUser");
        yield return Row<Action<AuthController, MfaRecoveryCodesRequestDto>>(nameof(AuthController.RotateMfaRecoveryCodes), "RequireUser");
        yield return Row<Action<AuthController, IMfaEnforcementPolicy>>(nameof(AuthController.GetMfaStatus), "RequireUser");

        yield return Row<Action<UserController>>(nameof(UserController.GetAllUsers), "RequireAdmin");
        yield return Row<Action<UserController>>(nameof(UserController.GetAccountSettings), "RequireUser");
        yield return Row<Action<UserController, UpdateAccountProfileRequestDto>>(nameof(UserController.UpdateAccountProfile), "RequireUser");
        yield return Row<Action<UserController, ConfirmAccountEmailChangeRequestDto>>(nameof(UserController.ConfirmAccountEmailChange), "RequireUser");
        yield return Row<Action<UserController, ChangePasswordRequestDto>>(nameof(UserController.ChangePassword), "RequireUser");
        yield return Row<Action<UserController, ResetMfaRequestDto>>(nameof(UserController.ResetMfa), "RequireUser");
        yield return Row<Action<UserController, DisableMfaRequestDto>>(nameof(UserController.DisableMfa), "RequireUser");
        yield return Row<Action<UserController, Guid>>(nameof(UserController.ResetUserMfa), AuthorizationPolicies.RequireKurinManagement);
        yield return Row<Action<UserController, Guid>>(nameof(UserController.DeleteUser), "RequireAdmin");
        yield return Row<Action<UserController, Guid, UserRole>>(nameof(UserController.ChangeUserRole), AuthorizationPolicies.RequireKurinManagement);

        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetByKey), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetAllByGroup), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetAllByKurin), "RequireUser");
        yield return Row<Action<MemberController, UpsertMemberRequest, CancellationToken>>(nameof(MemberController.Create), "RequireUser");
        yield return Row<Action<MemberController, Guid, UpsertMemberRequest, CancellationToken>>(nameof(MemberController.Update), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.Delete), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetKurinKvMembers), "RequireUser");

        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetByKey), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.Exists), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetAll), "RequireUser");
        yield return Row<Action<GroupController, CreateGroupRequest>>(nameof(GroupController.Create), "RequireUser");
        yield return Row<Action<GroupController, Guid, UpdateGroupRequest>>(nameof(GroupController.Update), "RequireUser");
        yield return Row<Action<GroupController, Guid, UploadImageRequest, CancellationToken>>(nameof(GroupController.UploadSilhouette), "RequireUser");
        yield return Row<Action<GroupController, Guid, CancellationToken>>(nameof(GroupController.DeleteSilhouette), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.Delete), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetMentors), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetKurinMentorAssignments), "RequireUser");

        yield return Row<Action<KurinController, Guid>>(nameof(KurinController.GetByKey), "RequireUser");
        yield return Row<Action<KurinController>>(nameof(KurinController.GetAll), "RequireAdmin");
        yield return Row<Action<KurinController, int>>(nameof(KurinController.Create), "RequireAdmin");
        yield return Row<Action<KurinController, Guid, UpdateKurinRequest>>(nameof(KurinController.Upsert), "RequireUser");
        yield return Row<Action<KurinController, Guid>>(nameof(KurinController.Delete), "RequireUser");

        yield return Row<Action<LeadershipController, string, Guid, CancellationToken>>(nameof(LeadershipController.GetLeadershipByType), "RequireUser");
        yield return Row<Action<LeadershipController, Guid>>(nameof(LeadershipController.GetLeadershipByKey), "RequireUser");
        yield return Row<Action<LeadershipController, UpsertLeadershipRequest>>(nameof(LeadershipController.CreateLeadership), "RequireUser");
        yield return Row<Action<LeadershipController, Guid, UpsertLeadershipRequest>>(nameof(LeadershipController.UpdateLeadership), "RequireUser");
        yield return Row<Action<LeadershipController, Guid>>(nameof(LeadershipController.GetLeadershipHistories), "RequireUser");

        yield return Row<Action<PlanningController, CreatePlanningSession>>(nameof(PlanningController.CreatePlanningSession), "RequirePlanningAuthor");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.GetPlanningSessionByKey), "RequireUser");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.GetPlanningSessions), "RequireUser");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.DeletePlanningSession), "RequireUser");

        yield return Row<Action<BadgesCatalogController>>(nameof(BadgesCatalogController.GetMetadata), "RequireUser");
        yield return Row<Action<BadgesCatalogController, int>>(nameof(BadgesCatalogController.GetAll), "RequireUser");
        yield return Row<Action<BadgesCatalogController, string>>(nameof(BadgesCatalogController.GetById), "RequireUser");

        yield return Row<Action<ProbesCatalogController>>(nameof(ProbesCatalogController.GetAll), "RequireUser");
        yield return Row<Action<ProbesCatalogController, string>>(nameof(ProbesCatalogController.GetGroupedById), "RequireUser");

        yield return Row<Action<MemberProgressController, Guid>>(nameof(MemberProgressController.GetBadgeProgresses), "RequireUser");
        yield return Row<Action<MemberProgressController, Guid, string, SubmitBadgeProgressRequest>>(nameof(MemberProgressController.SubmitBadgeProgress), "RequireUser");
        yield return Row<Action<MemberProgressController, Guid, string, ReviewBadgeProgressRequest>>(nameof(MemberProgressController.ReviewBadgeProgress), AuthorizationPolicies.RequireGroupLeadership);
        yield return Row<Action<MemberProgressController, Guid, string>>(nameof(MemberProgressController.GetProbeProgress), "RequireUser");
        yield return Row<Action<MemberProgressController, Guid, string, UpdateProbeProgressStatusRequest>>(nameof(MemberProgressController.UpdateProbeProgressStatus), "RequireUser");

        // Agenda reads are open to the kurin; raising an item is a провід capability, while editing or
        // dropping one is settled per item by ResourceAuthorize (author, or the Виховник it targets).
        yield return Row<Action<AgendaController, Guid, DateTime?, DateTime?>>(nameof(AgendaController.GetCalendar), "RequireUser");
        yield return Row<Action<AgendaController, Guid>>(nameof(AgendaController.GetBoard), "RequireUser");
        yield return Row<Action<AgendaController, Guid>>(nameof(AgendaController.GetAssignTargets), "RequireAgendaAuthor");
        yield return Row<Action<AgendaController, CreateAgendaItem>>(nameof(AgendaController.Create), "RequireAgendaAuthor");
        yield return Row<Action<AgendaController, Guid, UpdateAgendaItem>>(nameof(AgendaController.Update), "RequireUser");
        yield return Row<Action<AgendaController, Guid, ChangeAgendaStatusRequest>>(nameof(AgendaController.ChangeStatus), "RequireUser");
        yield return Row<Action<AgendaController, Guid>>(nameof(AgendaController.Delete), "RequireUser");
        yield return Row<Action<AgendaController, Guid>>(nameof(AgendaController.GetCategories), "RequireUser");
        yield return Row<Action<AgendaController, Guid>>(nameof(AgendaController.GetCategoriesForManagement), "RequireUser");
        yield return Row<Action<AgendaController, UpsertAgendaCategory>>(nameof(AgendaController.UpsertCategory), "RequireUser");
        yield return Row<Action<AgendaController, Guid, UpsertAgendaCategory>>(nameof(AgendaController.UpdateCategory), "RequireUser");
        yield return Row<Action<AgendaController, Guid, Guid>>(nameof(AgendaController.DeleteCategory), "RequireUser");
        yield return Row<Action<AgendaController, Guid>>(nameof(AgendaController.GetResponses), "RequireUser");
        yield return Row<Action<AgendaController, Guid, SetAgendaResponseRequest>>(nameof(AgendaController.SetResponse), "RequireUser");

        yield return Endpoint<AuthController>(nameof(AuthController.SetKurinScope), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<GroupController>(nameof(GroupController.AssignMentor), "RequireUser");
        yield return Endpoint<GroupController>(nameof(GroupController.RevokeMentor), "RequireUser");
        yield return Endpoint<KurinController>(nameof(KurinController.ExportReportPdf), AuthorizationPolicies.RequireKurinManagement);
        yield return Endpoint<KurinController>(nameof(KurinController.GetBadgeReviewQueue), AuthorizationPolicies.RequireGroupLeadership);
        yield return Endpoint<MemberAwardsController>(nameof(MemberAwardsController.DeleteAward), AuthorizationPolicies.RequireUser);
        yield return Endpoint<MemberAwardsController>(nameof(MemberAwardsController.ReviewAward), AuthorizationPolicies.RequireGroupLeadership);
        yield return Endpoint<MemberAwardsController>(nameof(MemberAwardsController.UpsertAward), AuthorizationPolicies.RequireUser);
        yield return Endpoint<MemberController>(nameof(MemberController.CreateByKurin), "RequireUser");
        yield return Endpoint<MemberController>(nameof(MemberController.GetKurinMentorCandidates), AuthorizationPolicies.RequireKurinManagement);
        yield return Endpoint<MemberController>(nameof(MemberController.ResetProfileVerification), AuthorizationPolicies.RequireGroupLeadership);
        yield return Endpoint<MemberController>(nameof(MemberController.VerifyProfile), AuthorizationPolicies.RequireGroupLeadership);
        yield return Endpoint<MemberProgressController>(nameof(MemberProgressController.SignProbePoint), "RequireUser");
        yield return Endpoint<MemberProgressController>(nameof(MemberProgressController.UnsignProbePoint), "RequireUser");
        yield return Endpoint<MemberWarningsController>(nameof(MemberWarningsController.AssignWarning), "RequireUser");
        yield return Endpoint<MemberWarningsController>(nameof(MemberWarningsController.CancelWarning), "RequireUser");
        yield return Endpoint<MemberWarningsController>(nameof(MemberWarningsController.GetWarnings), AuthorizationPolicies.RequireUser);
        yield return Endpoint<MigrationController>(nameof(MigrationController.GetPreflightReport), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<NotificationsController>(nameof(NotificationsController.GetInbox), AuthorizationPolicies.RequireUser);
        yield return Endpoint<NotificationsController>(nameof(NotificationsController.GetUnreadCount), AuthorizationPolicies.RequireUser);
        yield return Endpoint<NotificationsController>(nameof(NotificationsController.MarkAllAsRead), AuthorizationPolicies.RequireUser);
        yield return Endpoint<NotificationsController>(nameof(NotificationsController.MarkAsRead), AuthorizationPolicies.RequireUser);
        yield return Endpoint<OnboardingController>(nameof(OnboardingController.ApproveWaitlistEntry), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<OnboardingController>(nameof(OnboardingController.GetOnboardingStats), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<OnboardingController>(nameof(OnboardingController.GetWaitlistEntries), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<OnboardingController>(nameof(OnboardingController.RejectWaitlistEntry), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<OnboardingController>(nameof(OnboardingController.ResendInvitation), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.Approve), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.Create), AdminOrServiceTokenRequirement.PolicyName);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.Delete), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.DeleteImage), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.GetAll), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.GetByKey), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.GetCleanupStatus), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.Preview), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.Publish), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.Reject), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.SubmitForApproval), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.Update), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.UploadImage), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<SettingsController>(nameof(SettingsController.GetSettings), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<SettingsController>(nameof(SettingsController.UpdateSetting), AuthorizationPolicies.RequireAdmin);
        yield return Endpoint<UserController>(nameof(UserController.GetTileLayouts), AuthorizationPolicies.RequireUser);
        yield return Endpoint<UserController>(nameof(UserController.ResetTileLayout), AuthorizationPolicies.RequireUser);
        yield return Endpoint<UserController>(nameof(UserController.SaveTileLayout), AuthorizationPolicies.RequireUser);
    }

    public static IEnumerable<object[]> AllowAnonymousEndpoints()
    {
        yield return Row<Action<AuthController, LoginUserRequest>>(nameof(AuthController.Login));
        yield return Row<Action<AuthController>>(nameof(AuthController.Refresh));
        yield return Row<Action<AuthController, MfaLoginRequestDto>>(nameof(AuthController.VerifyMfaLogin));

yield return AnonymousEndpoint<AuthController>(nameof(AuthController.LoadTestLogin));
        yield return AnonymousEndpoint<E2ETestController>(nameof(E2ETestController.GetLatestInvitationByEmail));
        yield return AnonymousEndpoint<E2ETestController>(nameof(E2ETestController.Reset));
        yield return AnonymousEndpoint<MemberAwardsController>(nameof(MemberAwardsController.GetAwardImage));
        yield return AnonymousEndpoint<OnboardingController>(nameof(OnboardingController.ActivateAccount));
        yield return AnonymousEndpoint<OnboardingController>(nameof(OnboardingController.RequestPasswordReset));
        yield return AnonymousEndpoint<OnboardingController>(nameof(OnboardingController.ResetPassword));
        yield return AnonymousEndpoint<OnboardingController>(nameof(OnboardingController.SubmitWaitlistRegistration));
        yield return AnonymousEndpoint<OnboardingController>(nameof(OnboardingController.ValidateInvitationToken));
        yield return AnonymousEndpoint<PublicAnnouncementsController>(nameof(PublicAnnouncementsController.GetImage));
        yield return AnonymousEndpoint<SetupController>(nameof(SetupController.GetStatus));
        yield return AnonymousEndpoint<SetupController>(nameof(SetupController.Initialize));
    }

    private static object[] Row<TDelegate>(string methodName)
    {
        var action = ResolveMethod<TDelegate>(methodName);
        return [action];
    }

    private static object[] Row<TDelegate>(string methodName, string policy)
    {
        var action = ResolveMethod<TDelegate>(methodName);
        return [action, policy];
    }

    /// <summary>
    /// A row for an action whose name is unique on its controller — most of them. The delegate form
    /// below stays for the handful that are overloaded; writing one out for all fifty-seven endpoints
    /// is how they stayed unlisted in the first place.
    /// </summary>
    private static object[] Endpoint<TController>(string methodName, string policy)
    {
        return [ResolveUnique<TController>(methodName), policy];
    }

    private static object[] AnonymousEndpoint<TController>(string methodName)
    {
        return [ResolveUnique<TController>(methodName)];
    }

    private static MethodInfo ResolveUnique<TController>(string methodName)
    {
        var candidates = typeof(TController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == methodName)
            .ToArray();

        return candidates.Length switch
        {
            1 => candidates[0],
            0 => throw new InvalidOperationException($"{typeof(TController).Name} has no action '{methodName}'."),
            _ => throw new InvalidOperationException(
                $"{typeof(TController).Name}.{methodName} is overloaded; use the delegate form to pick one.")
        };
    }

    private static MethodInfo ResolveMethod<TDelegate>(string methodName)
    {
        var invoke = typeof(TDelegate).GetMethod("Invoke")!;
        var parameters = invoke.GetParameters();

        if (parameters.Length == 0)
        {
            throw new InvalidOperationException("Delegate must include controller parameter.");
        }

        var controllerType = parameters[0].ParameterType;
        var actionParameterTypes = parameters.Skip(1).Select(p => p.ParameterType).ToArray();

        var method = controllerType.GetMethod(methodName, actionParameterTypes);
        return method ?? throw new InvalidOperationException(
            $"Unable to resolve method '{controllerType.Name}.{methodName}({string.Join(",", actionParameterTypes.Select(t => t.Name))})'.");
    }
}
