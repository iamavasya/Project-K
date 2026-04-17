using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models
{
    public sealed record GroupedProbeResponse(
        string Id,
        string Title,
        int PointsCount,
        int SectionsCount,
        IReadOnlyList<ProbeSection> Sections
    )
    {
        public static GroupedProbeResponse FromProbe(Probe probe)
        {
            return new GroupedProbeResponse(
                probe.Id,
                probe.Title,
                probe.Points.Count,
                probe.Sections?.Count ?? 0,
                probe.Sections ?? Array.Empty<ProbeSection>()
            );
        }
    }
}
