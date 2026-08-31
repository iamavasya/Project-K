using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using ProjectK.API.Authorization;
using ProjectK.API.Controllers.KurinModule;
using ProjectK.API.Helpers;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Tests.Security;

/// <summary>
/// Two gates guard most endpoints: a coarse policy ("is the caller this kind of office") and the
/// resource check ("may this caller act on this record"). They are written independently, per
/// endpoint, so they drift — and a policy that refuses an office the permission map allows produces
/// a 403 nothing else explains.
/// <para>
/// The rule pinned here is the one that broke: <b>reading a record must never require more than
/// changing it.</b> <c>GetLeadershipByKey</c> demanded whole-kurin management while
/// <c>UpdateLeadership</c> asked only for a signed-in user, so Курінний — who may seat the offices
/// below him — could not open the record he was allowed to edit. The edit page died on its first
/// request and every existing test stayed green.
/// </para>
/// <para>
/// Only an endpoint's <b>own subject</b> counts — the resource its controller is named for, reached
/// by the same key. Half the API checks <c>Kurin</c> merely to ask "is this your kurin?" while the
/// endpoint is about agenda categories or planning; comparing that scope carrier across unrelated
/// features says nothing about who may read what.
/// </para>
/// <para>
/// Stated over policies rather than over one endpoint, so the next endpoint that inverts read and
/// write fails here instead of in somebody's browser.
/// </para>
/// </summary>
public class PolicyMatchesPermissionMapTests
{
    private static readonly ResourceAction[] WriteActions =
        [ResourceAction.Create, ResourceAction.Update, ResourceAction.Delete, ResourceAction.Manage];

    [Fact]
    public void ReadingAResource_ShouldNeverRequireMoreThanChangingIt()
    {
        var endpoints = ControllerActions()
            .SelectMany(action => StaticResourceChecks(action)
                .Where(check => IsControllerSubject(action, check.Resource))
                .Select(check => (action, check.Resource, check.Action, check.KeySelector, Policy: PolicyOf(action))))
            .Where(endpoint => endpoint.Policy != null)
            .ToList();

        var offences = new List<string>();

        foreach (var read in endpoints.Where(endpoint => endpoint.Action == ResourceAction.Read))
        {
            var readmitted = RolesAdmittedBy(read.Policy!);

            foreach (var write in endpoints.Where(endpoint =>
                         endpoint.Resource == read.Resource
                         && endpoint.KeySelector == read.KeySelector
                         && WriteActions.Contains(endpoint.Action)))
            {
                var refused = RolesAdmittedBy(write.Policy!).Except(readmitted).ToList();
                if (refused.Count > 0)
                {
                    offences.Add(
                        $"{read.action.DeclaringType!.Name}.{read.action.Name} reads {read.Resource} behind "
                        + $"{read.Policy}, but {write.action.DeclaringType!.Name}.{write.action.Name} lets "
                        + $"{write.Policy} change it — {string.Join(", ", refused)} may write what they cannot read.");
                }
            }
        }

        Assert.True(offences.Count == 0, string.Join(Environment.NewLine, offences));
    }

    /// <summary>Every system role the named policy lets through, admin excluded — admin passes everything.</summary>
    private static IReadOnlyCollection<string> RolesAdmittedBy(string policy)
    {
        Func<IEnumerable<string>, bool> grants = policy switch
        {
            AuthorizationPolicies.RequireKurinManagement => RolePermissionMap.GrantsWholeKurinManagement,
            AuthorizationPolicies.RequireGroupLeadership => RolePermissionMap.GrantsGroupLeadership,
            AuthorizationPolicies.RequireAgendaAuthor => RolePermissionMap.GrantsAgendaAuthoring,
            AuthorizationPolicies.RequirePlanningAuthor => RolePermissionMap.GrantsPlanningAuthoring,
            AuthorizationPolicies.RequireUser => _ => true,
            _ => _ => false
        };

        return SystemRole.All()
            .Where(role => role != SystemRole.Admin)
            .Where(role => grants([role]))
            .ToList();
    }

    /// <summary>
    /// Whether the checked resource is what the controller is about, rather than a container it
    /// happens to scope against. <c>LeadershipController</c> checking <c>Leadership</c> is its
    /// subject; <c>PlanningController</c> checking <c>Kurin</c> is a scope carrier.
    /// </summary>
    private static bool IsControllerSubject(MethodInfo action, ResourceType resource) =>
        action.DeclaringType!.Name.Contains(resource.ToString(), StringComparison.Ordinal);

    private static string? PolicyOf(MethodInfo action) =>
        (action.GetCustomAttribute<AuthorizeAttribute>()
         ?? action.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>())?.Policy;

    private static IEnumerable<MethodInfo> ControllerActions() =>
        typeof(LeadershipController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() == null);

    /// <summary>
    /// The (resource, action) pairs an endpoint checks, for the attributes naming the resource
    /// outright. The selector form resolves its type from the request, so there is nothing static to
    /// compare.
    /// </summary>
    private static IEnumerable<(ResourceType Resource, ResourceAction Action, string KeySelector)> StaticResourceChecks(
        MethodInfo action)
    {
        foreach (var attribute in action.GetCustomAttributes<ResourceAuthorizeAttribute>())
        {
            if (attribute.Arguments is
                [bool hasStaticType, ResourceType resource, _, ResourceAction resourceAction, string keySelector, ..]
                && hasStaticType)
            {
                yield return (resource, resourceAction, keySelector);
            }
        }
    }
}
