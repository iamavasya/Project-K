using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.Infrastructure.Tests.KurinModule.RepositoryTests.Integration;

/// <summary>
/// A person is in a kurin because a membership says so. The fixtures used to rely on the columns on
/// the member record alone, which no read looks at any more, so they have to say it here too.
/// </summary>
internal static class Placing
{
    internal static Membership Of(Member member) => new()
    {
        MemberKey = member.MemberKey,
        UserKey = member.UserKey,
        KurinKey = member.KurinKey,
        GroupKey = member.GroupKey,
        Kind = MembershipKind.Youth,
        JoinedAtUtc = DateTime.UtcNow
    };
}
