using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Reports;
using Xunit;

namespace ProjectK.API.Tests.Reports;

/// <summary>
/// The report's label tables must answer for every enum value.
/// <para>
/// The frontend keeps its own tables — deliberately, because it renders different wording for the
/// same values (the report says "Прихильник", the member list says "пл. прих."). What must not
/// happen is a new office or level being added on one side and silently falling back to its raw
/// enum name on the other, so each side asserts its own table is total.
/// </para>
/// </summary>
public class KurinReportTerminologyTotalityTests
{
    public static TheoryData<LeadershipRole> LeadershipRoles()
    {
        var data = new TheoryData<LeadershipRole>();
        foreach (var role in Enum.GetValues<LeadershipRole>())
        {
            data.Add(role);
        }

        return data;
    }

    public static TheoryData<PlastLevel> PlastLevels()
    {
        var data = new TheoryData<PlastLevel>();
        foreach (var level in Enum.GetValues<PlastLevel>())
        {
            data.Add(level);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(LeadershipRoles))]
    public void EveryLeadershipRole_HasALabel(LeadershipRole role)
    {
        var label = KurinReportTerminology.LeadershipRole(role);

        Assert.True(label != role.ToString(), $"{role} would print as its enum name in the report.");
    }

    [Theory]
    [MemberData(nameof(PlastLevels))]
    public void EveryPlastLevel_HasALabel(PlastLevel level)
    {
        var label = KurinReportTerminology.PlastLevel(level);

        Assert.True(label != level.ToString(), $"{level} would print as its enum name in the report.");
    }
}
