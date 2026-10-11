namespace ProjectK.Common.Models.Dtos.InfrastructureModule;

/// <summary>The server's VAPID public key (base64url), or <c>null</c> when push is not set up here.</summary>
public sealed class WebPushPublicKeyDto
{
    public string? PublicKey { get; set; }
}

/// <summary>A browser's <c>PushSubscription.toJSON()</c>, as the PWA posts it.</summary>
public sealed class WebPushSubscriptionRequestDto
{
    public string? Endpoint { get; set; }
    public WebPushKeysDto? Keys { get; set; }
}

public sealed class WebPushKeysDto
{
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
}
