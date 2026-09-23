using Microsoft.Extensions.Configuration;
using ProjectK.Infrastructure.Services.JwtService;

namespace ProjectK.Infrastructure.Tests.Services;

/// <summary>
/// The purpose tokens: each is read back only for the purpose it was minted for, and the MFA
/// trust token carries the stamp that ties it to the account as it was.
/// </summary>
public class JwtServiceTests
{
    private static JwtService Service(int? trustDays = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "unit-test-signing-key-that-is-long-enough-for-hmac-sha256",
            ["Jwt:Issuer"] = "lileyka-tests",
            ["Jwt:Audience"] = "lileyka",
            ["Jwt:ExpiresInMinutes"] = "15"
        };
        if (trustDays is { } days)
        {
            settings["Security:MfaTrustDays"] = days.ToString();
        }

        return new JwtService(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), TimeProvider.System);
    }

    [Fact]
    public void MfaTrust_ShouldRoundTripTheAccountAndItsStamp()
    {
        var service = Service();
        var userId = Guid.NewGuid();

        var grant = service.GenerateMfaTrustToken(userId, "stamp-1");
        var trust = service.ReadMfaTrust(grant.Token);

        Assert.NotNull(trust);
        Assert.Equal(userId, trust!.UserId);
        Assert.Equal("stamp-1", trust.SecurityStamp);
    }

    [Fact]
    public void MfaTrust_ShouldLiveAsLongAsConfigured_AndSevenDaysByDefault()
    {
        var now = DateTime.UtcNow;

        var configured = Service(trustDays: 30).GenerateMfaTrustToken(Guid.NewGuid(), "s");
        var @default = Service().GenerateMfaTrustToken(Guid.NewGuid(), "s");

        Assert.InRange(configured.ExpiresUtc, now.AddDays(30).AddMinutes(-1), now.AddDays(30).AddMinutes(1));
        Assert.InRange(@default.ExpiresUtc, now.AddDays(7).AddMinutes(-1), now.AddDays(7).AddMinutes(1));
    }

    [Fact]
    public void PurposeTokens_ShouldNotBeReadAsOneAnother()
    {
        var service = Service();
        var userId = Guid.NewGuid();

        var challenge = service.GenerateMfaChallengeToken(userId);
        var trust = service.GenerateMfaTrustToken(userId, "stamp-1").Token;
        var ticket = service.GenerateDevReturnTicket(userId);

        Assert.Null(service.ReadMfaTrust(challenge));
        Assert.Null(service.ReadMfaTrust(ticket));
        Assert.Null(service.ReadMfaChallenge(trust));
        Assert.Null(service.ReadDevReturnTicket(trust));
        Assert.Equal(userId, service.ReadMfaChallenge(challenge));
    }

    [Fact]
    public void MfaTrust_ShouldRefuseAForgedToken()
    {
        Assert.Null(Service().ReadMfaTrust("not-a-token"));
        Assert.Null(Service().ReadMfaTrust(string.Empty));
    }
}
