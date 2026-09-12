using System.Net;
using Microsoft.Extensions.Options;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Settings;
using Resend;

namespace ProjectK.Infrastructure.Services.EmailService;

/// <summary>
/// The letters Лілейка sends, in Ukrainian, under one frame: the banner on top, one green button,
/// a plain copy of the link for clients that strip buttons, and a footer that says why the letter
/// came. Every colour is inline, because mail clients read no stylesheet.
/// </summary>
public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly EmailSettings _settings;

    public ResendEmailService(IResend resend, IOptions<EmailSettings> settings)
    {
        _resend = resend;
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage();
        message.From = $"{_settings.FromName} <{_settings.FromEmail}>";
        message.To.Add(to);
        message.Subject = subject;
        message.HtmlBody = body;

        await _resend.EmailSendAsync(message, cancellationToken);
    }

    public async Task SendInvitationEmailAsync(string to, string token, CancellationToken cancellationToken = default)
    {
        var activationUrl = $"{_settings.BaseUrl}/activate/{token}";
        var body = Frame(
            title: "Вас запрошено до Лілейки",
            paragraphs:
            [
                "Для вас створено акаунт у Лілейці — системі обліку куреня. Щоб активувати його і встановити пароль, натисніть кнопку нижче.",
                $"Посилання діє {OnboardingPolicy.InvitationLifetimeDays} днів. Якщо воно прострочиться, нове можна замовити на сторінці входу через «Забули пароль».",
            ],
            buttonText: "Активувати акаунт",
            url: activationUrl,
            footer: "Якщо ви не чекали цього листа, просто не звертайте на нього уваги — без активації акаунт не запрацює.");

        await SendEmailAsync(to, "Лілейка · запрошення до системи", body, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string to, string token, CancellationToken cancellationToken = default)
    {
        // Both encoded: a reset token carries '+' and '/', and an address may too, and either
        // one read back from the query string as a space breaks the link for exactly that person.
        var resetUrl = $"{_settings.BaseUrl}/reset-password?token={WebUtility.UrlEncode(token)}&email={WebUtility.UrlEncode(to)}";
        var body = Frame(
            title: "Відновлення пароля",
            paragraphs:
            [
                "Ми отримали запит на зміну пароля до вашого акаунта в Лілейці. Щоб обрати новий пароль, натисніть кнопку нижче.",
                "Посилання діє обмежений час.",
            ],
            buttonText: "Встановити новий пароль",
            url: resetUrl,
            footer: "Якщо ви не просили змінити пароль, нічого робити не треба — ваш пароль лишається чинним.");

        await SendEmailAsync(to, "Лілейка · відновлення пароля", body, cancellationToken);
    }

    /// <summary>
    /// One frame for every letter. Table layout on purpose: it is the only thing every mail
    /// client lays out the same way.
    /// </summary>
    private string Frame(string title, IReadOnlyList<string> paragraphs, string buttonText, string url, string footer)
    {
        var bannerUrl = $"{_settings.BaseUrl}/assets/images/email-banner.png";
        var encodedUrl = WebUtility.HtmlEncode(url);
        var text = string.Join(
            string.Empty,
            paragraphs.Select(paragraph =>
                $"<p style='margin:0 0 16px;font-size:16px;line-height:1.5;color:#1F2A27;'>{WebUtility.HtmlEncode(paragraph)}</p>"));

        return $@"<!DOCTYPE html>
<html lang='uk'>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1' />
  <title>{WebUtility.HtmlEncode(title)}</title>
</head>
<body style='margin:0;padding:0;background:#F7F9F8;'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#F7F9F8;padding:24px 0;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='600' cellpadding='0' cellspacing='0' style='max-width:600px;width:100%;background:#FFFFFF;border-radius:12px;overflow:hidden;font-family:Arial,Helvetica,sans-serif;'>
          <tr>
            <td style='background:#0E6E4E;'>
              <img src='{bannerUrl}' width='600' alt='Лілейка' style='display:block;width:100%;max-width:600px;height:auto;border:0;' />
            </td>
          </tr>
          <tr>
            <td style='padding:32px 32px 8px;'>
              <h1 style='margin:0 0 16px;font-size:22px;line-height:1.3;color:#0E6E4E;'>{WebUtility.HtmlEncode(title)}</h1>
              {text}
              <table role='presentation' cellpadding='0' cellspacing='0' style='margin:24px 0;'>
                <tr>
                  <td style='background:#0E6E4E;border-radius:8px;'>
                    <a href='{encodedUrl}' style='display:inline-block;padding:12px 28px;color:#FBF8F1;font-size:16px;font-weight:bold;text-decoration:none;'>{WebUtility.HtmlEncode(buttonText)}</a>
                  </td>
                </tr>
              </table>
              <p style='margin:0 0 8px;font-size:14px;line-height:1.5;color:#5B6763;'>Якщо кнопка не працює, скопіюйте це посилання в браузер:</p>
              <p style='margin:0 0 24px;font-size:13px;line-height:1.5;word-break:break-all;'><a href='{encodedUrl}' style='color:#0E6E4E;'>{encodedUrl}</a></p>
              <p style='margin:0 0 24px;font-size:14px;line-height:1.5;color:#5B6763;'>Не бачите наших листів? Перевірте теку «Спам» і додайте адресу відправника до контактів.</p>
            </td>
          </tr>
          <tr>
            <td style='padding:16px 32px 28px;border-top:1px solid #E3E8E6;'>
              <p style='margin:0;font-size:12px;line-height:1.5;color:#8A9490;'>{WebUtility.HtmlEncode(footer)}</p>
              <p style='margin:8px 0 0;font-size:12px;line-height:1.5;color:#8A9490;'>Лілейка · система обліку куреня. Лист надіслано автоматично, відповідати на нього не потрібно.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
