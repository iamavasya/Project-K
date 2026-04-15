using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using ProjectK.API.Controllers.AuthModule;
using ProjectK.API.Controllers.KurinModule;
using ProjectK.API.Controllers.ProbesAndBadgesModule;
using ProjectK.API.Controllers.UsersModule;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Create;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;
using ProjectK.Common.Models.Dtos.Requests;

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

    [Theory]
    [MemberData(nameof(TemporarilyUnprotectedEndpoints))]
    public void Endpoint_ShouldBeTemporarilyUnprotected(MethodInfo action)
    {
        var authorize = action.GetCustomAttribute<AuthorizeAttribute>()
            ?? action.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>();
        var allowAnonymous = action.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.Null(authorize);
        Assert.Null(allowAnonymous);
    }

    public static IEnumerable<object[]> PolicyEndpoints()
    {
        yield return Row<Action<AuthController, RegisterUserRequest>>(nameof(AuthController.RegisterManager), "RequireAdmin");
        yield return Row<Action<AuthController, RegisterUserRequest>>(nameof(AuthController.Register), "RequireManager");
        yield return Row<Action<AuthController>>(nameof(AuthController.Logout), "RequireUser");
        yield return Row<Action<AuthController, CheckEntityAccessRequest>>(nameof(AuthController.CheckAccess), "RequireUser");

        yield return Row<Action<UserController>>(nameof(UserController.GetAllUsers), "RequireAdmin");

        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetByKey), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetAllByGroup), "RequireUser");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetAllByKurin), "RequireUser");
        yield return Row<Action<MemberController, UpsertMemberRequest, CancellationToken>>(nameof(MemberController.Create), "RequireMentor");
        yield return Row<Action<MemberController, Guid, UpsertMemberRequest, CancellationToken>>(nameof(MemberController.Update), "RequireMentor");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.Delete), "RequireMentor");
        yield return Row<Action<MemberController, Guid>>(nameof(MemberController.GetKurinKvMembers), "RequireManager");

        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetByKey), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.Exists), "RequireUser");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.GetAll), "RequireUser");
        yield return Row<Action<GroupController, CreateGroupRequest>>(nameof(GroupController.Create), "RequireMentor");
        yield return Row<Action<GroupController, Guid, UpdateGroupRequest>>(nameof(GroupController.Update), "RequireMentor");
        yield return Row<Action<GroupController, Guid>>(nameof(GroupController.Delete), "RequireManager");

        yield return Row<Action<KurinController, Guid>>(nameof(KurinController.GetByKey), "RequireUser");
        yield return Row<Action<KurinController>>(nameof(KurinController.GetAll), "RequireAdmin");
        yield return Row<Action<KurinController, int>>(nameof(KurinController.Create), "RequireAdmin");
        yield return Row<Action<KurinController, Guid, int>>(nameof(KurinController.Upsert), "RequireManager");
        yield return Row<Action<KurinController, Guid>>(nameof(KurinController.Delete), "RequireManager");

        yield return Row<Action<LeadershipController, string, Guid, CancellationToken>>(nameof(LeadershipController.GetLeadershipByType), "RequireUser");
        yield return Row<Action<LeadershipController, Guid>>(nameof(LeadershipController.GetLeadershipByKey), "RequireManager");
        yield return Row<Action<LeadershipController, UpsertLeadershipRequest>>(nameof(LeadershipController.CreateLeadership), "RequireManager");
        yield return Row<Action<LeadershipController, Guid, UpsertLeadershipRequest>>(nameof(LeadershipController.UpdateLeadership), "RequireManager");
        yield return Row<Action<LeadershipController, Guid>>(nameof(LeadershipController.GetLeadershipHistories), "RequireManager");

        yield return Row<Action<PlanningController, CreatePlanningSession>>(nameof(PlanningController.CreatePlanningSession), "RequireManager");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.GetPlanningSessionByKey), "RequireManager");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.GetPlanningSessions), "RequireManager");
        yield return Row<Action<PlanningController, Guid>>(nameof(PlanningController.DeletePlanningSession), "RequireManager");
    }

    public static IEnumerable<object[]> AllowAnonymousEndpoints()
    {
        yield return Row<Action<AuthController, LoginUserRequest>>(nameof(AuthController.Login));
        yield return Row<Action<AuthController>>(nameof(AuthController.Refresh));
    }

    public static IEnumerable<object[]> TemporarilyUnprotectedEndpoints()
    {
        // Stage 0 baseline: catalog endpoints are currently open by design.
        yield return Row<Action<BadgesCatalogController>>(nameof(BadgesCatalogController.GetMetadata));
        yield return Row<Action<BadgesCatalogController, int>>(nameof(BadgesCatalogController.GetAll));
        yield return Row<Action<BadgesCatalogController, string>>(nameof(BadgesCatalogController.GetById));

        yield return Row<Action<ProbesCatalogController>>(nameof(ProbesCatalogController.GetAll));
        yield return Row<Action<ProbesCatalogController, string>>(nameof(ProbesCatalogController.GetGroupedById));
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
