using System.Collections.Generic;
using System.Linq;
using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Models.Enums;

/// <summary>
/// How a гілка and a senior kurin's numbers read in a letter or a notification. The web mirrors it in
/// <c>kurin-label.function.ts</c>.
/// </summary>
public static class KurinBranchNames
{
    public static string Label(KurinBranch branch) => branch switch
    {
        KurinBranch.USP => "УСП",
        KurinBranch.UPS => "УПС",
        _ => "УПЮ"
    };

    /// <summary>«5-й курінь УСП · 20-й курінь УПС».</summary>
    public static string DescribeSenior(IEnumerable<KurinNumbering> branches)
        => string.Join(" · ", KurinBranchSet.Ordered(branches, b => b.Branch)
            .Select(b => b.Number is int number ? $"{number}-й курінь {Label(b.Branch)}" : $"курінь {Label(b.Branch)}"));
}
