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
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.API.Models.Requests;

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
    /// Endpoints the baseline never covered. They are pinned so the gap cannot grow silently, not
    /// because leaving them unlisted is right — each still needs a row.
    /// </summary>
    private static readonly IReadOnlySet<string> KnownUnlistedEndpoints = new HashSet<string>
    {
        "AuthController.LoadTestLogin", "AuthController.SetKurinScope",
        "E2ETestController.GetLatestInvitationByEmail", "E2ETestController.Reset",
        "GroupController.AssignMentor", "GroupController.RevokeMentor",
        "KurinController.ExportReportPdf", "KurinController.GetBadgeReviewQueue",
        "MemberAwardsController.DeleteAward", "MemberAwardsController.GetAwardImage",
        "MemberAwardsController.ReviewAward", "MemberAwardsController.UpsertAward",
        "MemberController.CreateByKurin", "MemberController.GetKurinMentorCandidates",
        "MemberController.ResetProfileVerification", "MemberController.VerifyProfile",
        "MemberProgressController.SignProbePoint", "MemberProgressController.UnsignProbePoint",
        "MemberWarningsController.AssignWarning", "MemberWarningsController.CancelWarning",
        "MemberWarningsController.GetWarnings", "MigrationController.GetPreflightReport",
        "NotificationsController.GetInbox", "NotificationsController.GetUnreadCount",
        "NotificationsController.MarkAllAsRead", "NotificationsController.MarkAsRead",
        "OnboardingController.ActivateAccount", "OnboardingController.ApproveWaitlistEntry",
        "OnboardingController.GetOnboardingStats", "OnboardingController.GetWaitlistEntries",
        "OnboardingController.RejectWaitlistEntry", "OnboardingController.RequestPasswordReset",
        "OnboardingController.ResendInvitation", "OnboardingController.ResetPassword",
        "OnboardingController.SubmitWaitlistRegistration", "OnboardingController.ValidateInvitationToken",
        "PublicAnnouncementsController.Approve", "PublicAnnouncementsController.Create",
        "PublicAnnouncementsController.Delete", "PublicAnnouncementsController.DeleteImage",
        "PublicAnnouncementsController.GetAll", "PublicAnnouncementsController.GetByKey",
        "PublicAnnouncementsController.GetCleanupStatus", "PublicAnnouncementsController.GetImage",
        "PublicAnnouncementsController.Preview", "PublicAnnouncementsController.Publish",
        "PublicAnnouncementsController.Reject", "PublicAnnouncementsController.SubmitForApproval",
        "PublicAnnouncementsController.Update", "PublicAnnouncementsController.UploadImage",
        "SettingsController.GetSettings", "SettingsController.UpdateSetting",
        "SetupController.GetStatus", "SetupController.Initialize",
        "UserController.GetTileLayouts", "UserController.ResetTileLayout", "UserController.SaveTileLayout"
    };

    public static IEnumerable<object[]> PolicyEndpoints()
    {
        yield return Row<Action<AuthController, RegisterUserRequest>>(nameof(AuthController.RegisterManager), "RequireAdmin");
        yield return Row<Action<AuthController, RegisterUserRequest>>(nameof(AuthController.Register), "RequireManager");
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
        yield return Row<Action<UserController, Guid>>(nameof(UserController.ResetUserMfa), "RequireManager");
        yield return Row<Action<UserController, Guid>>(nameof(UserController.DeleteUser), "RequireAdmin");
        yield return Row<Action<UserController, Guid, UserRole>>(nameof(UserController.ChangeUserRole), "RequireManager");

        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetByKey), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetAllByGroup), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetAllByKurin), "RequireUser");
        yield return Row<Action<MemberController, UpsertMemberRequest, CancellationToken>>(nameof(MemberController.Create), "RequireMentor");
        yield return Row<Action<MemberController, Guid, UpsertMemberRequest, CancellationToken>>(nameof(MemberController.Update), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.Delete), "RequireMentor");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetKurinKvMembers), "RequireUser");

        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetByKey), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.Exists), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetAll), "RequireUser");
        yield return Row<Action<GroupController, CreateGroupRequest>>(nameof(GroupController.Create), "RequireMentor");
        yield return Row<Action<GroupController, Guid, UpdateGroupRequest>>(nameof(GroupController.Update), "RequireMentor");
        yield return Row<Action<GroupController, Guid, IFormFile, CancellationToken>>(nameof(GroupController.UploadSilhouette), "RequireMentor");
        yield return Row<Action<GroupController, Guid, CancellationToken>>(nameof(GroupController.DeleteSilhouette), "RequireMentor");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.Delete), "RequireManager");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetMentors), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetKurinMentorAssignments), "RequireUser");

        yield return Row<Action<KurinController, Guid>>(nameof(KurinController.GetByKey), "RequireUser");
        yield return Row<Action<KurinController>>(nameof(KurinController.GetAll), "RequireAdmin");
        yield return Row<Action<KurinController, int>>(nameof(KurinController.Create), "RequireAdmin");
        yield return Row<Action<KurinController, Guid, UpdateKurinRequest>>(nameof(KurinController.Upsert), "RequireManager");
        yield return Row<Action<KurinController, Guid>>(nameof(KurinController.Delete), "RequireManager");

        yield return Row<Action<LeadershipController, string, Guid, CancellationToken>>(nameof(LeadershipController.GetLeadershipByType), "RequireUser");
        yield return Row<Action<LeadershipController, Guid>>(nameof(LeadershipController.GetLeadershipByKey), "RequireManager");
        yield return Row<Action<LeadershipController, UpsertLeadershipRequest>>(nameof(LeadershipController.CreateLeadership), "RequireUser");
        yield return Row<Action<LeadershipController, Guid, UpsertLeadershipRequest>>(nameof(LeadershipController.UpdateLeadership), "RequireUser");
        yield return Row<Action<LeadershipController, Guid>>(nameof(LeadershipController.GetLeadershipHistories), "RequireManager");

        yield return Row<Action<PlanningController, CreatePlanningSession>>(nameof(PlanningController.CreatePlanningSession), "RequirePlanningAuthor");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.GetPlanningSessionByKey), "RequireMentor");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.GetPlanningSessions), "RequireMentor");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.DeletePlanningSession), "RequireManager");

        yield return Row<Action<BadgesCatalogController>>(nameof(BadgesCatalogController.GetMetadata), "RequireUser");
        yield return Row<Action<BadgesCatalogController, int>>(nameof(BadgesCatalogController.GetAll), "RequireUser");
        yield return Row<Action<BadgesCatalogController, string>>(nameof(BadgesCatalogController.GetById), "RequireUser");

        yield return Row<Action<ProbesCatalogController>>(nameof(ProbesCatalogController.GetAll), "RequireUser");
        yield return Row<Action<ProbesCatalogController, string>>(nameof(ProbesCatalogController.GetGroupedById), "RequireUser");

        yield return Row<Action<MemberProgressController, Guid>>(nameof(MemberProgressController.GetBadgeProgresses), "RequireUser");
        yield return Row<Action<MemberProgressController, Guid, string, SubmitBadgeProgressRequest>>(nameof(MemberProgressController.SubmitBadgeProgress), "RequireUser");
        yield return Row<Action<MemberProgressController, Guid, string, ReviewBadgeProgressRequest>>(nameof(MemberProgressController.ReviewBadgeProgress), "RequireMentor");
        yield return Row<Action<MemberProgressController, Guid, string>>(nameof(MemberProgressController.GetProbeProgress), "RequireUser");
        yield return Row<Action<MemberProgressController, Guid, string, UpdateProbeProgressStatusRequest>>(nameof(MemberProgressController.UpdateProbeProgressStatus), "RequireMentor");

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
    }

    public static IEnumerable<object[]> AllowAnonymousEndpoints()
    {
        yield return Row<Action<AuthController, LoginUserRequest>>(nameof(AuthController.Login));
        yield return Row<Action<AuthController>>(nameof(AuthController.Refresh));
        yield return Row<Action<AuthController, MfaLoginRequestDto>>(nameof(AuthController.VerifyMfaLogin));
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
