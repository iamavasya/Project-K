using FluentAssertions;
using ProjectK.Common.Models.Records;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule;

/// <summary>
/// The code is derived, not drawn: the same person always has the same one, and the migration that
/// backfilled existing members computes it in SQL. These tests pin the shape both sides must agree on.
/// </summary>
public class MemberPublicIdTests
{
    [Fact]
    public void For_ShouldTakeTheFirstTenDigitsOfTheKey_Grouped()
    {
        var memberKey = Guid.Parse("562fd596-f9b2-44ee-ac02-b6544a9ed439");

        MemberPublicId.For(memberKey).Should().Be("PL-562FD-596F9");
    }

    [Fact]
    public void For_ShouldBeStable_ForTheSameKey()
    {
        var memberKey = Guid.NewGuid();

        MemberPublicId.For(memberKey).Should().Be(MemberPublicId.For(memberKey));
    }

    [Fact]
    public void For_ShouldUseNoAmbiguousCharacters()
    {
        // Hexadecimal only: nothing here can be misheard as another character when read aloud.
        var code = MemberPublicId.For(Guid.NewGuid());

        code.Should().MatchRegex("^PL-[0-9A-F]{5}-[0-9A-F]{5}$");
    }
}
