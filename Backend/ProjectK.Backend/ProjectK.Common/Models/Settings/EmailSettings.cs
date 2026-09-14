namespace ProjectK.Common.Models.Settings;

public class EmailSettings
{
    public string Provider { get; set; } = "Mock"; // "Mock" or "Resend"
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "onboarding@resend.dev";
    public string FromName { get; set; } = "Лілейка";
    /// <summary>A mailbox a person can answer to; empty leaves the letters without Reply-To.</summary>
    public string ReplyTo { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "http://localhost:4200";
}
