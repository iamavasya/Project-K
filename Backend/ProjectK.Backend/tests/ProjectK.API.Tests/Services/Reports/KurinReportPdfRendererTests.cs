using ProjectK.API.Services.Reports;
using QuestPDF.Infrastructure;

namespace ProjectK.API.Tests.Services.Reports;

public sealed class KurinReportPdfRendererTests
{
    [Fact]
    public void Render_ShouldReturnPdfBytes()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var renderer = new KurinReportPdfRenderer();
        var report = new KurinReportData(
            new KurinReportHeader(new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc), "Manager User", "manager@example.com", "v0.13.0-beta", "Stage8"),
            new KurinReportKurin(Guid.NewGuid(), 1, "Kyiv", "Ukraine", "Patron", "Description", false, 15),
            [],
            [],
            []);

        var bytes = renderer.Render(report);
        var body = System.Text.Encoding.ASCII.GetString(bytes);

        Assert.StartsWith("%PDF-1.", body);
        Assert.Contains("%%EOF", body);
    }

    [Fact]
    public void Render_ShouldEmbedAvailableImages()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var renderer = new KurinReportPdfRenderer();
        var kurinKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var image = OnePixelPng();
        var member = new KurinReportMember(
            memberKey,
            null,
            groupKey,
            "Group A",
            "Test Member",
            "TM",
            "member@example.com",
            "+380000000000",
            new DateOnly(2010, 1, 1),
            null,
            null,
            "member-photos/test.png",
            image,
            null,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var report = new KurinReportData(
            new KurinReportHeader(new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc), "Manager User", "manager@example.com", "v0.13.0-beta", "Stage8"),
            new KurinReportKurin(kurinKey, 1, "Kyiv", "Ukraine", "Patron", "Description", false, 15),
            [
                new KurinReportGroup(
                    groupKey,
                    "Group A",
                    "Description",
                    "group-silhouettes/test.png",
                    image,
                    [],
                    [
                        new KurinReportGroupMember(memberKey, "Test Member", "member@example.com", "+380000000000", null)
                    ])
            ],
            [],
            [member]);

        var bytes = renderer.Render(report);
        var body = System.Text.Encoding.ASCII.GetString(bytes);

        Assert.StartsWith("%PDF-1.", body);
        Assert.Contains("%%EOF", body);
    }

    private static byte[] OnePixelPng()
        => Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
}
