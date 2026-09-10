namespace ProjectK.Common.Models.Settings;

public sealed class TelegramOptions
{
    public TelegramDevAlertOptions DevAlerts { get; set; } = new();
}

public sealed class TelegramDevAlertOptions
{
    public bool Enabled { get; set; }

    public string? BotToken { get; set; }

    public string? ChatId { get; set; }

    public string BaseUrl { get; set; } = "https://api.telegram.org";

    public int TimeoutSeconds { get; set; } = 10;

    public bool DisableNotification { get; set; }

    public string MinimumLevel { get; set; } = "Warning";

    public int MaxMessageLength { get; set; } = 3500;
}
