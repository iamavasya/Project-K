using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Services.Reports;

public static class KurinReportTerminology
{
    public static string PlastLevel(PlastLevel? value)
        => value is null ? "-" : PlastLevelLabels.GetValueOrDefault(value.Value, value.Value.ToString());

    public static string ProbeStatus(ProbeProgressStatus value)
        => ProbeStatusLabels.GetValueOrDefault(value, value.ToString());

    public static string BadgeStatus(BadgeProgressStatus value)
        => BadgeStatusLabels.GetValueOrDefault(value, value.ToString());

    public static string WarningLevel(MemberWarningLevel value)
        => WarningLevelLabels.GetValueOrDefault(value, value.ToString());

    public static string AwardLevel(MemberAwardLevel value)
        => AwardLevelLabels.GetValueOrDefault(value, value.ToString());

    public static string LeadershipType(LeadershipType value)
        => LeadershipTypeLabels.GetValueOrDefault(value, value.ToString());

    public static string LeadershipRole(LeadershipRole value)
        => LeadershipRoleLabels.GetValueOrDefault(value, value.ToString());

    private static readonly IReadOnlyDictionary<PlastLevel, string> PlastLevelLabels =
        new Dictionary<PlastLevel, string>
        {
            [ProjectK.Common.Models.Enums.PlastLevel.Entry] = "Прихильник",
            [ProjectK.Common.Models.Enums.PlastLevel.Uchasnyk] = "Учасник",
            [ProjectK.Common.Models.Enums.PlastLevel.Rozviduvach] = "Розвідувач",
            [ProjectK.Common.Models.Enums.PlastLevel.Skob] = "Скоб",
            [ProjectK.Common.Models.Enums.PlastLevel.HetmanskiySkob] = "Гетьманський скоб",
            [ProjectK.Common.Models.Enums.PlastLevel.Starshoplastun] = "Старшопластун",
            [ProjectK.Common.Models.Enums.PlastLevel.Senior] = "Сеньйор",
            [ProjectK.Common.Models.Enums.PlastLevel.SeniorPratsi] = "Сеньйор праці",
            [ProjectK.Common.Models.Enums.PlastLevel.SeniorDovirja] = "Сеньйор довір'я",
            [ProjectK.Common.Models.Enums.PlastLevel.SeniorKerivnytstva] = "Сеньйор керівництва"
        };

    private static readonly IReadOnlyDictionary<ProbeProgressStatus, string> ProbeStatusLabels =
        new Dictionary<ProbeProgressStatus, string>
        {
            [ProjectK.Common.Models.Enums.ProbeProgressStatus.NotStarted] = "Не розпочато",
            [ProjectK.Common.Models.Enums.ProbeProgressStatus.InProgress] = "В процесі",
            [ProjectK.Common.Models.Enums.ProbeProgressStatus.Completed] = "Підписано",
            [ProjectK.Common.Models.Enums.ProbeProgressStatus.Verified] = "Підтверджено"
        };

    private static readonly IReadOnlyDictionary<BadgeProgressStatus, string> BadgeStatusLabels =
        new Dictionary<BadgeProgressStatus, string>
        {
            [ProjectK.Common.Models.Enums.BadgeProgressStatus.Draft] = "Чернетка",
            [ProjectK.Common.Models.Enums.BadgeProgressStatus.Submitted] = "Подано на підтвердження",
            [ProjectK.Common.Models.Enums.BadgeProgressStatus.Confirmed] = "Підтверджено",
            [ProjectK.Common.Models.Enums.BadgeProgressStatus.Rejected] = "Відхилено"
        };

    private static readonly IReadOnlyDictionary<MemberWarningLevel, string> WarningLevelLabels =
        new Dictionary<MemberWarningLevel, string>
        {
            [ProjectK.Common.Models.Enums.MemberWarningLevel.Level1] = "1-а пересторога",
            [ProjectK.Common.Models.Enums.MemberWarningLevel.Level2] = "2-а пересторога",
            [ProjectK.Common.Models.Enums.MemberWarningLevel.Level3] = "3-а пересторога"
        };

    private static readonly IReadOnlyDictionary<MemberAwardLevel, string> AwardLevelLabels =
        new Dictionary<MemberAwardLevel, string>
        {
            [ProjectK.Common.Models.Enums.MemberAwardLevel.First] = "1-е відзначення",
            [ProjectK.Common.Models.Enums.MemberAwardLevel.Second] = "2-е відзначення",
            [ProjectK.Common.Models.Enums.MemberAwardLevel.Third] = "3-е відзначення",
            [ProjectK.Common.Models.Enums.MemberAwardLevel.Fourth] = "4-е відзначення"
        };

    private static readonly IReadOnlyDictionary<LeadershipType, string> LeadershipTypeLabels =
        new Dictionary<LeadershipType, string>
        {
            [ProjectK.Common.Models.Enums.LeadershipType.Kurin] = "Курінь",
            [ProjectK.Common.Models.Enums.LeadershipType.Group] = "Гурток",
            [ProjectK.Common.Models.Enums.LeadershipType.KV] = "КВ"
        };

    private static readonly IReadOnlyDictionary<LeadershipRole, string> LeadershipRoleLabels =
        new Dictionary<LeadershipRole, string>
        {
            [ProjectK.Common.Models.Enums.LeadershipRole.Kurinnuy] = "Курінний",
            [ProjectK.Common.Models.Enums.LeadershipRole.Hurtkoviy] = "Гуртковий",
            [ProjectK.Common.Models.Enums.LeadershipRole.Suddya] = "Суддя",
            [ProjectK.Common.Models.Enums.LeadershipRole.Pysar] = "Писар",
            [ProjectK.Common.Models.Enums.LeadershipRole.Skarbnyk] = "Скарбник",
            [ProjectK.Common.Models.Enums.LeadershipRole.Horunjiy] = "Хорунжий",
            [ProjectK.Common.Models.Enums.LeadershipRole.Gospodar] = "Господар",
            [ProjectK.Common.Models.Enums.LeadershipRole.Hronikar] = "Хронікар",
            [ProjectK.Common.Models.Enums.LeadershipRole.Instruktor] = "Інструктор",
            [ProjectK.Common.Models.Enums.LeadershipRole.Vykhovnyk] = "Впорядник",
            [ProjectK.Common.Models.Enums.LeadershipRole.Zvyazkovyi] = "Зв'язковий"
        };
}
