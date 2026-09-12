using Microsoft.Extensions.Options;
using Moq;
using ProjectK.Common.Models.Settings;
using ProjectK.Infrastructure.Services.EmailService;
using Resend;
using Xunit.Abstractions;

namespace ProjectK.Infrastructure.Tests.Services;

/// <summary>
/// The letters people actually receive: in Ukrainian, under the banner, with the link both as a
/// button and as text, and the address of the sender the settings name.
/// </summary>
public class ResendEmailServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly List<EmailMessage> _sent = [];
    private readonly ResendEmailService _service;

    public ResendEmailServiceTests(ITestOutputHelper output)
    {
        _output = output;
        var resend = new Mock<IResend>();
        resend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback((EmailMessage message, CancellationToken _) => _sent.Add(message))
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), null!));
        var settings = Options.Create(new EmailSettings
        {
            FromEmail = "lileyka@example.com",
            FromName = "Лілейка",
            BaseUrl = "https://lileyka.example"
        });
        _service = new ResendEmailService(resend.Object, settings);
    }

    [Fact]
    public async Task Invitation_ShouldBeInUkrainian_UnderTheBanner_WithTheLinkThreeTimes()
    {
        await _service.SendInvitationEmailAsync("marta@example.com", "tok+en", CancellationToken.None);

        var message = Assert.Single(_sent);
        Assert.Equal("Лілейка <lileyka@example.com>", message.From);
        Assert.Contains("marta@example.com", message.To);
        Assert.Equal("Лілейка · запрошення до системи", message.Subject);
        Assert.Contains("https://lileyka.example/assets/images/email-banner.png", message.HtmlBody);
        // The button, the text link and the visible address: every client shows at least one.
        Assert.Equal(3, CountOf(message.HtmlBody!, "https://lileyka.example/activate/tok+en"));
        Assert.Contains("Активувати акаунт", message.HtmlBody);
        Assert.Contains("«Спам»", message.HtmlBody);
        Assert.DoesNotContain("Welcome", message.HtmlBody);
        _output.WriteLine("INVITATION-HTML-BEGIN");
        _output.WriteLine(message.HtmlBody);
        _output.WriteLine("INVITATION-HTML-END");
    }

    [Fact]
    public async Task PasswordReset_ShouldEncodeTheTokenAndAddress_AndSpeakUkrainian()
    {
        await _service.SendPasswordResetEmailAsync("marta+plast@example.com", "a+b/c=", CancellationToken.None);

        var message = Assert.Single(_sent);
        Assert.Equal("Лілейка · відновлення пароля", message.Subject);
        Assert.Contains("reset-password?token=a%2Bb%2Fc%3D&amp;email=marta%2Bplast%40example.com", message.HtmlBody);
        Assert.Contains("Встановити новий пароль", message.HtmlBody);
        Assert.DoesNotContain("Password Reset", message.HtmlBody);
    }

    private static int CountOf(string text, string needle)
    {
        var count = 0;
        for (var index = text.IndexOf(needle, StringComparison.Ordinal); index >= 0; index = text.IndexOf(needle, index + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
