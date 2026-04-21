namespace ProjectK.Common.Models.Settings
{
    public class EmailSettings
    {
        public string Provider { get; set; } = "Mock"; // "Mock" or "Resend"
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "onboarding@resend.dev";
        public string FromName { get; set; } = "ProjectK";
        public string BaseUrl { get; set; } = "http://localhost:4200";
    }
}
