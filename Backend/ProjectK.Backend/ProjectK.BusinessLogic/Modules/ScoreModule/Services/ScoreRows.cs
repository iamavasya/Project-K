using ProjectK.BusinessLogic.Modules.ScoreModule.Models;
using ProjectK.Common.Models.Score;

namespace ProjectK.BusinessLogic.Modules.ScoreModule.Services;

/// <summary>Ledger totals as the screens show them.</summary>
public static class ScoreRows
{
    public static ScoreGroupRowDto Group(ScoreGroupTotal total, int place, string name, bool canOpen) => new()
    {
        GroupKey = total.GroupKey,
        GroupName = name,
        Place = place,
        Score = total.Score,
        OtherScore = total.OtherScore,
        YouthPoints = total.YouthPoints,
        YouthCount = total.YouthCount,
        Average = total.Average,
        GroupPoints = total.GroupPoints,
        CanOpen = canOpen
    };
}
