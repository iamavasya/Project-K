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
    /// MM-03 (contracts replace the calls) and MM-06 (<c>Members</c> leaves <c>IUnitOfWork</c>).
    /// </summary>
    private static readonly string[] ReachesMemberDataDirectly =
    [
        "ProjectK.BusinessLogic.Modules.AuthModule.Features.Migration.PreflightReport.GetMigrationPreflightReportHandler",
        "ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ActivateAccount.ActivateAccountHandler",
        "ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.SubmitWaitlistRegistration.SubmitWaitlistRegistrationHandler",
        "ProjectK.BusinessLogic.Modules.AuthModule.Services.LeadershipRoleSyncService",
        "ProjectK.BusinessLogic.Modules.AuthModule.Services.LoginResponseFactory",
        "ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications.ReviewNotificationRecipientResolver",
        "ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Get.GetBadgeProgressesHandler",
        "ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Get.GetBadgeReviewQueueHandler",
        "ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Review.ReviewBadgeProgressHandler",
        "ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Submit.SubmitBadgeProgressHandler",
        "ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.Get.GetProbeProgressHandler",
        "ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.UpdatePointSignature.UpdateProbePointSignatureHandler",
        "ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.UpdateStatus.UpdateProbeProgressStatusHandler",
        "ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models.BadgeProgressResponse",
        "ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.ConfirmEmailChange.ConfirmAccountEmailChangeCommandHandler",
        "ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.Get.GetAccountSettingsQueryHandler",
        "ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.UpdateProfile.UpdateAccountProfileCommandHandler",
    ];

    /// <summary>
    /// The authorization types that still decide from the member record. Emptied by stage D, when the
    /// scope comes from <c>Membership</c> and the offices hang off it.
    /// </summary>
    private static readonly string[] AuthorizationStillKnowsAboutMember =
    [
        "ProjectK.BusinessLogic.Modules.AuthModule.Services.LeadershipRoleSyncService",
        "ProjectK.BusinessLogic.Modules.AuthModule.Services.LoginResponseFactory",
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
