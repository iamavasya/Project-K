using FluentAssertions;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ProjectK.Architecture.Tests;

/// <summary>
/// The four-project layering from CONTRIBUTING.md, as a test rather than a promise. These are green
/// today; they exist so that the edge which took a release to remove cannot come back in a review
/// nobody had time for.
/// </summary>
public class LayerRules
{
    private const string Common = @"ProjectK\.Common.*";
    private const string Infrastructure = @"ProjectK\.Infrastructure.*";
    private const string BusinessLogic = @"ProjectK\.BusinessLogic.*";
    private const string Api = @"ProjectK\.API.*";

    [Fact]
    public void Common_ShouldKnowNothingAboutTheOtherProjects()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching(Common)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Infrastructure)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(BusinessLogic)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Api);

        ProjectKArchitecture.Violations(rule).Should().BeEmpty();
    }

    [Fact]
    public void Infrastructure_ShouldNotReachIntoTheApplicationLayers()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching(Infrastructure)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(BusinessLogic)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Api);

        ProjectKArchitecture.Violations(rule).Should().BeEmpty();
    }

    [Fact]
    public void BusinessLogic_ShouldNotReachIntoInfrastructureOrTheHost()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching(BusinessLogic)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Infrastructure)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Api);

        ProjectKArchitecture.Violations(rule).Should().BeEmpty();
    }

    [Fact]
    public void BusinessLogic_ShouldNotSpeakEntityFramework()
    {
        var rule = Types()
            .That().ResideInNamespaceMatching(BusinessLogic)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(@"Microsoft\.EntityFrameworkCore.*");

        ProjectKArchitecture.Violations(rule).Should().BeEmpty();
    }

    /// <summary>
    /// The one controller that talks to the database itself: the e2e fixture endpoint, which exists to
    /// put a known state in front of Playwright. It is compiled into the same image as production and
    /// only configuration keeps it switched off — tracked as D-02 in the extraction backlog.
    /// </summary>
    private static readonly string[] TouchesTheDbContext =
    [
        "ProjectK.API.Controllers.TestModule.E2ETestController",
    ];

    [Fact]
    public void Api_ShouldNotTouchTheDbContextDirectly()
    {
        // Program is the composition root: registering the context and running migrations is its job.
        var rule = Types()
            .That().ResideInNamespaceMatching(Api)
            .And().DoNotHaveFullName("ProjectK.API.Program")
            .Should().NotDependOnAny(typeof(ProjectK.Infrastructure.DbContexts.AppDbContext));

        ProjectKArchitecture.Violations(rule).Should().BeEquivalentTo(TouchesTheDbContext);
    }
}
