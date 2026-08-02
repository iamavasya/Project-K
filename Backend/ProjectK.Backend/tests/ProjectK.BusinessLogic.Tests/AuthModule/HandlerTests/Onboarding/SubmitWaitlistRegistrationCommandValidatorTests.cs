using System;
using FluentValidation.TestHelper;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.AuthModule.HandlerTests.Onboarding;

public class SubmitWaitlistRegistrationCommandValidatorTests
{
    private readonly SubmitWaitlistRegistrationCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_ForValidCommand()
    {
        var result = _validator.TestValidate(CreateCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenStanytsiaMissing(string? stanytsia)
    {
        var result = _validator.TestValidate(CreateCommand(stanytsia: stanytsia));
        result.ShouldHaveValidationErrorFor(x => x.Stanytsia).WithErrorMessage("Stanytsia is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenStanytsiaTooLong()
    {
        var result = _validator.TestValidate(CreateCommand(stanytsia: new string('x', 121)));
        result.ShouldHaveValidationErrorFor(x => x.Stanytsia).WithErrorMessage("Stanytsia must be 120 characters or fewer.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldFail_WhenRegionMissing(string? region)
    {
        var result = _validator.TestValidate(CreateCommand(regionOrCountry: region));
        result.ShouldHaveValidationErrorFor(x => x.RegionOrCountry).WithErrorMessage("Region is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenKurinLeaderCandidateNotConfirmed()
    {
        var result = _validator.TestValidate(CreateCommand(isKurinLeaderCandidate: false));
        result.ShouldHaveValidationErrorFor(x => x.IsKurinLeaderCandidate)
            .WithErrorMessage("Kurin leader confirmation is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldFail_WhenKurinNumberMissing(string? number)
    {
        var result = _validator.TestValidate(CreateCommand(claimedKurinNumber: number));
        result.ShouldHaveValidationErrorFor(x => x.ClaimedKurinNameOrNumber)
            .WithErrorMessage("Kurin number is required.");
    }

    [Theory]
    [InlineData("97a")]
    [InlineData("Lisovi Chorty")]
    public void Validate_ShouldFail_WhenKurinNumberNotNumeric(string number)
    {
        var result = _validator.TestValidate(CreateCommand(claimedKurinNumber: number));
        result.ShouldHaveValidationErrorFor(x => x.ClaimedKurinNameOrNumber)
            .WithErrorMessage("Kurin number must contain only digits.");
    }

    private static SubmitWaitlistRegistrationCommand CreateCommand(
        string? stanytsia = "Kyiv",
        string? regionOrCountry = "Ukraine",
        bool isKurinLeaderCandidate = true,
        string? claimedKurinNumber = "97")
        => new(
            "Ihor",
            "Kovalenko",
            "ihor.kovalenko@example.com",
            "+38 (099) 111-22-33",
            new DateTime(1995, 5, 15),
            stanytsia,
            regionOrCountry,
            isKurinLeaderCandidate,
            claimedKurinNumber);
}
