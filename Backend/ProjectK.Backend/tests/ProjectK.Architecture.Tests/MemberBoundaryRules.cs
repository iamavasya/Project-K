using FluentAssertions;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ProjectK.Architecture.Tests;

/// <summary>
/// The boundary v0.20.0 is building. Both rules are red today, so each is asserted against a
/// baseline of the types that already cross the line: a new crossing fails immediately, and removing
/// one forces the list to shrink in the same commit. The release is finished when both lists are empty.
/// <para>
/// The rules read IL, not signatures — a handler that only ever touches a member through
/// <c>_unitOfWork.Members</c> inside a method body is caught just the same, and every entry below
/// is exactly that case.
/// </para>
/// </summary>
public class MemberBoundaryRules
{
    /// <summary>
    /// Everyone outside the member's own module who still reads member data directly. Emptied by
    /// MM-03: every one of them now asks <c>IMemberDirectory</c>. It stays empty — MM-06 removes the
    /// last way back in by taking <c>Members</c> off <c>IUnitOfWork</c>.
    /// </summary>
    private static readonly string[] ReachesMemberDataDirectly = [];

    /// <summary>
    /// What is left of authorization reading the member record: the scope reader, which still asks the
    /// member row which kurin and гурток it belongs to. Emptied by MM-11, when the scope comes from
    /// <c>Membership</c> instead.
    /// </summary>
    private static readonly string[] AuthorizationStillKnowsAboutMember =
    [
        "ProjectK.Infrastructure.Repositories.InfrastructureModule.ResourceScopeReader",
    ];

    [Fact]
    public void MemberData_ShouldBeReachedOnlyFromItsOwnModule()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching(@"ProjectK\.(BusinessLogic|API)\..*")
            .And().DoNotResideInNamespaceMatching(@"ProjectK\.BusinessLogic\.Modules\.KurinModule\..*")
            .And().DoNotResideInNamespaceMatching(@"ProjectK\.API\.Controllers\.KurinModule\..*")
            // AutoMapper profiles translate between an entity and its response, so naming both sides
            // is what they are for; they are not one module reading another's data.
            .And().DoNotResideInNamespaceMatching(@"ProjectK\.BusinessLogic\.MappingProfiles.*")
            .Should().NotDependOnAny(typeof(Member), typeof(IMemberRepository));

        ProjectKArchitecture.Violations(rule).Should().BeEquivalentTo(
            ReachesMemberDataDirectly,
            "the member module's boundary may shrink but never grow — update the baseline in the same commit");
    }

    [Fact]
    public void Authorization_ShouldNotKnowAboutMember()
    {
        var rule = Types()
            .That().ImplementInterface(typeof(IResourceAccessService))
            .Or().ImplementInterface(typeof(IResourceScopeReader))
            .Or().ImplementInterface(typeof(ILeadershipRoleSyncService))
            .Or().ImplementInterface(typeof(ILoginResponseFactory))
            .Should().NotDependOnAny(typeof(Member), typeof(IMemberRepository));

        ProjectKArchitecture.Violations(rule).Should().BeEquivalentTo(
            AuthorizationStillKnowsAboutMember,
            "who may act is decided from Membership and offices, never from the person's own record");
    }
}
