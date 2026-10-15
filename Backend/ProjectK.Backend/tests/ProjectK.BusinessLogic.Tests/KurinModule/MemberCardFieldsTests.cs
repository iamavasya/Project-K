using FluentAssertions;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.KraiProfiles;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule;

public class MemberCardFieldsTests
{
    private static readonly KraiCardFieldsSettings Prague = new(
        new Dictionary<string, string>
        {
            [KraiCardFields.MiddleName] = KraiCardFields.Hidden,
            [KraiCardFields.PhoneNumber] = KraiCardFields.Optional
        },
        [
            new KraiCardExtraField { Key = "visaType", Label = "Тип візи", Required = true },
            new KraiCardExtraField { Key = "visaUntil", Label = "Віза до", Type = KraiCardFields.Date }
        ]);

    private static UpsertMemberProfileCommand Write(Dictionary<string, string>? extras = null, string middleName = "", string phone = "") => new()
    {
        FirstName = "Ivan",
        LastName = "Petrenko",
        MiddleName = middleName,
        PhoneNumber = phone,
        CardExtras = extras
    };

    private static Member Existing(string? cardExtras) => new()
    {
        FirstName = "Ivan",
        LastName = "Petrenko",
        MiddleName = "Ivanovych",
        Email = "ivan@example.com",
        PhoneNumber = "+420 777 000 111",
        CardExtras = cardExtras
    };

    [Fact]
    public void WithoutAProfile_MiddleNameAndPhone_StayRequired()
    {
        var outcome = MemberCardFields.Apply(KraiCardFieldsSettings.Default, Write(middleName: "I"), null);

        outcome.Error.Should().Be("Поле «Номер телефону» обовʼязкове.");
    }

    [Fact]
    public void AHiddenField_KeepsWhatTheMemberHad()
    {
        var request = Write(new() { ["visaType"] = "D" }, middleName: "Changed");

        var outcome = MemberCardFields.Apply(Prague, request, Existing(null));

        outcome.Error.Should().BeNull();
        request.MiddleName.Should().Be("Ivanovych");
    }

    [Fact]
    public void ARequiredExtraField_MustBeFilled()
    {
        var outcome = MemberCardFields.Apply(Prague, Write(new() { ["visaType"] = "  " }), null);

        outcome.Error.Should().Be("Поле «Тип візи» обовʼязкове.");
    }

    [Fact]
    public void ExtraValues_AreMerged_AndAFieldTheKraiDroppedIsKept()
    {
        var outcome = MemberCardFields.Apply(
            Prague,
            Write(new() { ["visaUntil"] = "2027-06-30", ["unknown"] = "x" }),
            Existing("""{"visaType":"D","oldField":"kept"}"""));

        outcome.Error.Should().BeNull();
        MemberCardExtras.Read(outcome.CardExtras).Should().Equal(new Dictionary<string, string>
        {
            ["oldField"] = "kept",
            ["visaType"] = "D",
            ["visaUntil"] = "2027-06-30"
        });
    }

    [Fact]
    public void ADateField_TakesOnlyADate()
    {
        var outcome = MemberCardFields.Apply(Prague, Write(new() { ["visaType"] = "D", ["visaUntil"] = "30.06.2027" }), null);

        outcome.Error.Should().Be("Поле «Віза до» має бути датою.");
    }

    [Fact]
    public void AnEmptyValue_ClearsTheField()
    {
        var outcome = MemberCardFields.Apply(
            Prague,
            Write(new() { ["visaUntil"] = "" }),
            Existing("""{"visaType":"D","visaUntil":"2027-06-30"}"""));

        MemberCardExtras.Read(outcome.CardExtras).Should().Equal(new Dictionary<string, string> { ["visaType"] = "D" });
    }
}
