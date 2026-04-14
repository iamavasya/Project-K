using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models
{
    public sealed record ProbeSummaryResponse(
        string Id,
        string Title,
        int PointsCount,
        int SectionsCount
    )
    {
        public static ProbeSummaryResponse FromProbe(Probe probe)
        {
            return new ProbeSummaryResponse(
                probe.Id,
                probe.Title,
                probe.Points.Count,
                probe.Sections?.Count ?? 0
            );
        }
    }
}
