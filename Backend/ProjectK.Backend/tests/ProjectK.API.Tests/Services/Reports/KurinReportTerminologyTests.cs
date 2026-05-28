using ProjectK.API.Services.Reports;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Tests.Services.Reports;

public sealed class KurinReportTerminologyTests
{
    [Theory]
    [InlineData(MemberWarningLevel.Level1, "1-а пересторога")]
    [InlineData(MemberWarningLevel.Level2, "2-а пересторога")]
    [InlineData(MemberWarningLevel.Level3, "3-а пересторога")]
    public void WarningLevel_ShouldUseHumanReadableLabels(MemberWarningLevel level, string expected)
    {
        Assert.Equal(expected, KurinReportTerminology.WarningLevel(level));
    }

    [Theory]
    [InlineData(MemberAwardLevel.First, "1-е відзначення")]
    [InlineData(MemberAwardLevel.Second, "2-е відзначення")]
    [InlineData(MemberAwardLevel.Third, "3-е відзначення")]
    [InlineData(MemberAwardLevel.Fourth, "4-е відзначення")]
    public void AwardLevel_ShouldUseHumanReadableLabels(MemberAwardLevel level, string expected)
    {
        Assert.Equal(expected, KurinReportTerminology.AwardLevel(level));
    }

    [Fact]
    public void PlastLevel_ShouldUseHumanReadableLabels()
    {
        Assert.Equal("Скоб", KurinReportTerminology.PlastLevel(PlastLevel.Skob));
        Assert.Equal("-", KurinReportTerminology.PlastLevel(null));
    }
}
