namespace ProjectK.Common.Models.Enums
{
    /// <summary>
    /// The order a person passes the ступені in, and which of them a kurin of a given branch
    /// normally records. Order lives here rather than in the enum because the enum's numbers are
    /// storage and cannot be reordered — see <see cref="PlastLevel"/>.
    /// </summary>
    public static class PlastLadder
    {
        /// <summary>Every ступінь, in the order it is reached.</summary>
        public static readonly IReadOnlyList<PlastLevel> InOrder =
        [
            PlastLevel.Entry,
            PlastLevel.Prykhylnyk,
            PlastLevel.Uchasnyk,
            PlastLevel.Rozviduvach,
            PlastLevel.Skob,
            PlastLevel.HetmanskiySkob,
            PlastLevel.Starshoplastun,
            PlastLevel.Senior,
            PlastLevel.SeniorPratsi,
            PlastLevel.SeniorDovirja,
            PlastLevel.SeniorKerivnytstva
        ];

        private static readonly IReadOnlyList<PlastLevel> ThroughYouth =
        [
            PlastLevel.Entry,
            PlastLevel.Prykhylnyk,
            PlastLevel.Uchasnyk,
            PlastLevel.Rozviduvach,
            PlastLevel.Skob,
            PlastLevel.HetmanskiySkob
        ];

        private static readonly IReadOnlyList<PlastLevel> ThroughSenior =
        [
            .. ThroughYouth,
            PlastLevel.Starshoplastun
        ];

        /// <summary>
        /// What a kurin of this branch is shown by default. Cumulative on purpose: a УПС kurin still
        /// wants to see when its people were заприсяжені. This is a **default**, not a restriction —
        /// the провід turns individual columns on and off from there.
        /// </summary>
        public static IReadOnlyList<PlastLevel> DefaultFor(KurinBranch branch)
            => branch switch
            {
                KurinBranch.USP => ThroughSenior,
                KurinBranch.UPS => InOrder,
                _ => ThroughYouth
            };
    }
}
