using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace ProjectK.API.Services.Reports;

public sealed class KurinReportPdfRenderer
{
    public byte[] Render(KurinReportData report)
    {
        return Document.Create(document =>
        {
            ComposeSummaryPage(document, report);

            foreach (var group in report.Groups)
            {
                ComposeGroupPage(document, report, group);
            }

            foreach (var member in report.Members)
            {
                ComposeMemberPage(document, report, member);
            }
        }).GeneratePdf();
    }

    private static void ComposeSummaryPage(IDocumentContainer document, KurinReportData report)
    {
        document.Page(page =>
        {
            ConfigurePage(page, report);

            page.Content().Column(column =>
            {
                column.Spacing(14);
                column.Item().Element(container => ComposeKurinSummary(container, report));
                column.Item().Element(container => ComposeGroupsTable(container, report.Groups));
                column.Item().Element(container => ComposeKeyVolunteerSection(container, report.KeyVolunteers));
            });
        });
    }

    private static void ComposeGroupPage(IDocumentContainer document, KurinReportData report, KurinReportGroup group)
    {
        document.Page(page =>
        {
            ConfigurePage(page, report);

            page.Content().Column(column =>
            {
                column.Spacing(12);
                column.Item().Element(container => SectionHeader(container, $"Гурток: {group.Name}"));
                column.Item().Row(row =>
                {
                    row.ConstantItem(116).Element(container => ComposeGroupSilhouette(container, group));
                    row.RelativeItem().Element(container => KeyValueGrid(container, new[]
                    {
                        ("Сильветка", string.IsNullOrWhiteSpace(group.SilhouetteUrl) ? "Не завантажено" : group.SilhouetteUrl),
                        ("Впорядники", FormatList(group.MentorNames)),
                        ("Опис", group.Description ?? "-")
                    }));
                });
                column.Item().Element(container => ComposeGroupMembersTable(container, group.Members));
            });
        });
    }

    private static void ComposeMemberPage(IDocumentContainer document, KurinReportData report, KurinReportMember member)
    {
        document.Page(page =>
        {
            ConfigurePage(page, report);

            page.Content().Column(column =>
            {
                column.Spacing(10);
                column.Item().Element(container => SectionHeader(container, $"Картотека: {member.FullName}"));
                column.Item().Row(row =>
                {
                    row.ConstantItem(92).Element(container => ComposePhotoPlaceholder(container, member));
                    row.RelativeItem().Element(container => KeyValueGrid(container, new[]
                    {
                        ("Ініціали", member.Initials),
                        ("Гурток", member.GroupName ?? "-"),
                        ("Пошта", member.Email),
                        ("Телефон", member.PhoneNumber),
                        ("Дата народження", member.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                        ("Адреса", member.Address ?? "-"),
                        ("Школа", member.School ?? "-"),
                        ("Ступінь", KurinReportTerminology.PlastLevel(member.LatestPlastLevel)),
                        ("Ролі в системі", FormatList(member.SystemRoles))
                    }));
                });

                column.Item().Element(container => ComposeProbeState(container, member));
                column.Item().Element(container => ComposeBadges(container, member));
                column.Item().Element(container => ComposeWarningsAndAwards(container, member));
                column.Item().Element(container => ComposeLeadershipHistory(container, member));
            });
        });
    }

    private static void ConfigurePage(PageDescriptor page, KurinReportData report)
    {
        page.Size(PageSizes.A4);
        page.Margin(32);
        page.DefaultTextStyle(style => style.FontSize(9).FontFamily("Arial"));

        page.Header().Element(container => ComposePageHeader(container, report));
        page.Footer().AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
            text.Span("Сторінка ");
            text.CurrentPageNumber();
            text.Span(" з ");
            text.TotalPages();
        });
    }

    private static void ComposePageHeader(IContainer container, KurinReportData report)
    {
        container.PaddingBottom(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"Курінь #{report.Kurin.Number}").FontSize(15).Bold();
                column.Item().Text("Beta export звіту").FontSize(8).SemiBold().FontColor(Colors.Orange.Darken2);
                column.Item().Text($"Звіт сформовано: {FormatDateTime(report.Header.GeneratedAtUtc)} UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text(report.Header.GeneratedByName).FontSize(9).SemiBold();
                if (!string.IsNullOrWhiteSpace(report.Header.GeneratedByEmail))
                {
                    column.Item().AlignRight().Text(report.Header.GeneratedByEmail).FontSize(8).FontColor(Colors.Grey.Darken1);
                }

                column.Item().AlignRight().Text($"{report.Header.BackendVersion} \"{report.Header.BackendCodename}\"")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void ComposeKurinSummary(IContainer container, KurinReportData report)
    {
        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Element(item => SectionHeader(item, "Інформація куреня"));
            column.Item().Row(row =>
            {
                row.ConstantItem(110).Element(item => ComposeKurinSilhouette(item, report.Kurin.Number));
                row.RelativeItem().Element(item => KeyValueGrid(item, new[]
                {
                    ("Число куреня", report.Kurin.Number.ToString(CultureInfo.InvariantCulture)),
                    ("Станиця", report.Kurin.Stanytsia ?? "-"),
                    ("Край / країна", report.Kurin.RegionOrCountry ?? "-"),
                    ("Ім. кого", report.Kurin.NamedAfter ?? "-"),
                    ("ЗБТ", report.Kurin.IsZbtKurin ? $"Так, ліміт {report.Kurin.ZbtUserCap}" : "Ні"),
                    ("Опис", report.Kurin.Description ?? "-")
                }));
            });
        });
    }

    private static void ComposeKurinSilhouette(IContainer container, int number)
    {
        container
            .Width(90)
            .Height(120)
            .Border(2)
            .BorderColor(Colors.Blue.Darken2)
            .Background(Colors.Blue.Lighten5)
            .AlignCenter()
            .AlignMiddle()
            .Text(number.ToString(CultureInfo.InvariantCulture))
            .FontSize(42)
            .Bold()
            .FontColor(Colors.Blue.Darken2);
    }

    private static void ComposePhotoPlaceholder(IContainer container, KurinReportMember member)
    {
        if (member.ProfilePhotoBytes is { Length: > 0 })
        {
            container
                .Width(82)
                .Height(108)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Image(member.ProfilePhotoBytes)
                .FitArea();
            return;
        }

        container.Width(82).Height(108).Element(item => ComposeInitialsPlaceholder(
            item,
            member.Initials,
            string.IsNullOrWhiteSpace(member.ProfilePhotoUrl) ? "Фото не завантажено" : "Фото недоступне"));
    }

    private static void ComposeGroupSilhouette(IContainer container, KurinReportGroup group)
    {
        if (group.SilhouetteBytes is { Length: > 0 })
        {
            container
                .Width(96)
                .Height(96)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Image(group.SilhouetteBytes)
                .FitArea();
            return;
        }

        container.Width(96).Height(96).Element(item => ComposeInitialsPlaceholder(
            item,
            group.Name.Length > 0 ? group.Name[..1].ToUpperInvariant() : "-",
            string.IsNullOrWhiteSpace(group.SilhouetteUrl) ? "Сильветка не завантажена" : "Сильветка недоступна"));
    }

    private static void ComposeInitialsPlaceholder(IContainer container, string initials, string caption)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Background(Colors.Grey.Lighten4)
            .Padding(6)
            .Column(column =>
            {
                column.Item().AlignCenter().Text(initials).FontSize(20).Bold().FontColor(Colors.Grey.Darken2);
                column.Item().PaddingTop(6).Text(caption)
                    .FontSize(7)
                    .FontColor(Colors.Grey.Darken1)
                    .AlignCenter();
            });
    }

    private static void ComposeGroupsTable(IContainer container, IReadOnlyList<KurinReportGroup> groups)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Element(item => SectionHeader(item, "Список гуртків"));

            if (groups.Count == 0)
            {
                column.Item().Text("Гуртків немає.").FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1);
                });

                HeaderCell(table, "№");
                HeaderCell(table, "Назва");
                HeaderCell(table, "Впорядник");
                HeaderCell(table, "Учас.");

                for (var i = 0; i < groups.Count; i++)
                {
                    var group = groups[i];
                    BodyCell(table, (i + 1).ToString(CultureInfo.InvariantCulture));
                    BodyCell(table, group.Name);
                    BodyCell(table, FormatList(group.MentorNames));
                    BodyCell(table, group.Members.Count.ToString(CultureInfo.InvariantCulture));
                }
            });
        });
    }

    private static void ComposeKeyVolunteerSection(IContainer container, IReadOnlyList<KurinReportMember> members)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Element(item => SectionHeader(item, "КВ, зв'язкові та впорядники"));

            if (members.Count == 0)
            {
                column.Item().Text("Ключових користувачів не знайдено.").FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                HeaderCell(table, "ПІБ");
                HeaderCell(table, "Пошта");
                HeaderCell(table, "Телефон");
                HeaderCell(table, "Ролі");

                foreach (var member in members)
                {
                    BodyCell(table, member.FullName);
                    BodyCell(table, member.Email);
                    BodyCell(table, member.PhoneNumber);
                    BodyCell(table, FormatList(member.SystemRoles));
                }
            });
        });
    }

    private static void ComposeGroupMembersTable(IContainer container, IReadOnlyList<KurinReportGroupMember> members)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Element(item => SectionHeader(item, "Учасники гуртка"));

            if (members.Count == 0)
            {
                column.Item().Text("Учасників немає.").FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                HeaderCell(table, "ПІБ");
                HeaderCell(table, "Пошта");
                HeaderCell(table, "Телефон");
                HeaderCell(table, "Ступінь");

                foreach (var member in members)
                {
                    BodyCell(table, member.FullName);
                    BodyCell(table, member.Email);
                    BodyCell(table, member.PhoneNumber ?? "-");
                    BodyCell(table, KurinReportTerminology.PlastLevel(member.LatestPlastLevel));
                }
            });
        });
    }

    private static void ComposeProbeState(IContainer container, KurinReportMember member)
    {
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(item => SectionHeader(item, "Стан проби"));

            if (member.Probes.Count == 0 && member.SignedProbePoints.Count == 0)
            {
                column.Item().Text("Прогрес проб не зафіксовано.").FontColor(Colors.Grey.Darken1);
                return;
            }

            foreach (var probe in member.Probes)
            {
                column.Item().Text($"{probe.ProbeTitle}: {probe.StatusLabel}; підписав: {probe.CompletedByName ?? "-"}; підтвердив: {probe.VerifiedByName ?? "-"}");
            }

            if (member.SignedProbePoints.Count > 0)
            {
                column.Item().PaddingTop(2).Text("Підписані точки").SemiBold();
                foreach (var point in member.SignedProbePoints)
                {
                    column.Item().Text($"{point.ProbeTitle}, {point.PointLabel}: {FormatDateTime(point.SignedAtUtc)}; {point.SignedByName ?? "-"} ({point.SignedByRole ?? "-"})");
                }
            }
        });
    }

    private static void ComposeBadges(IContainer container, KurinReportMember member)
    {
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(item => SectionHeader(item, "Підтверджені вмілості"));

            if (member.ConfirmedBadges.Count == 0)
            {
                column.Item().Text("Підтверджених вмілостей немає.").FontColor(Colors.Grey.Darken1);
                return;
            }

            foreach (var badge in member.ConfirmedBadges)
            {
                column.Item().Text($"{badge.BadgeTitle}: {badge.StatusLabel}; підтвердив: {badge.ReviewedByName ?? "-"} ({badge.ReviewedByRole ?? "-"})");
            }
        });
    }

    private static void ComposeWarningsAndAwards(IContainer container, KurinReportMember member)
    {
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(item => SectionHeader(item, "Перестороги та відзначення"));

            if (member.ActiveWarnings.Count == 0)
            {
                column.Item().Text("Активних пересторог немає.").FontColor(Colors.Grey.Darken1);
            }
            else
            {
                foreach (var warning in member.ActiveWarnings)
                {
                    column.Item().Text($"{warning.LevelLabel}: {FormatDateTime(warning.IssuedAtUtc)} - {FormatDateTime(warning.ExpiresAtUtc)}");
                }
            }

            if (member.Awards.Count == 0)
            {
                column.Item().Text("Відзначень немає.").FontColor(Colors.Grey.Darken1);
            }
            else
            {
                foreach (var award in member.Awards)
                {
                    column.Item().Text($"{award.LevelLabel}: {FormatDateTime(award.DateAcquired)}; {award.StatusLabel}; {award.Note ?? "-"}");
                }
            }
        });
    }

    private static void ComposeLeadershipHistory(IContainer container, KurinReportMember member)
    {
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(item => SectionHeader(item, "Історія діловодств"));

            if (member.LeadershipHistory.Count == 0)
            {
                column.Item().Text("Історії діловодств немає.").FontColor(Colors.Grey.Darken1);
                return;
            }

            foreach (var history in member.LeadershipHistory)
            {
                column.Item().Text($"{history.TypeLabel}/{history.RoleLabel}; {history.ScopeName ?? "-"}; {history.StartDate:yyyy-MM-dd} - {history.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "дотепер"}");
            }
        });
    }

    private static void KeyValueGrid(IContainer container, IEnumerable<(string Label, string Value)> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(90);
                columns.RelativeColumn();
            });

            foreach (var (label, value) in rows)
            {
                table.Cell().Element(KeyCell).Text(label).SemiBold();
                table.Cell().Element(ValueCell).Text(value);
            }
        });
    }

    private static void SectionHeader(IContainer container, string title)
    {
        container
            .PaddingVertical(4)
            .BorderBottom(1)
            .BorderColor(Colors.Blue.Lighten2)
            .Text(title)
            .FontSize(12)
            .Bold()
            .FontColor(Colors.Blue.Darken2);
    }

    private static void HeaderCell(TableDescriptor table, string value)
    {
        table.Cell()
            .Element(cell => cell.Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4))
            .Text(value)
            .SemiBold();
    }

    private static void BodyCell(TableDescriptor table, string value)
    {
        table.Cell()
            .Element(cell => cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4))
            .Text(value);
    }

    private static IContainer KeyCell(IContainer container)
        => container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingRight(6);

    private static IContainer ValueCell(IContainer container)
        => container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3);

    private static string FormatList(IReadOnlyCollection<string> values)
        => values.Count == 0 ? "-" : string.Join(", ", values);

    private static string FormatDateTime(DateTime? value)
        => value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "-";
}
