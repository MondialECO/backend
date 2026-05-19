using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebApp.Models;
using Xunit;

namespace WebApp.Tests.Integration;

/// <summary>
/// Verifies the per-IP auth rate-limit policy actually rejects with 429
/// after the configured permit limit. Uses its own AppFixture (separate
/// app/host) so rate-limit state is isolated from other test classes.
/// </summary>
public class AuthRateLimitIntegrationTests : IClassFixture<AppFixture>
{
    private readonly AppFixture _fx;

    public AuthRateLimitIntegrationTests(AppFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Login_returns_429_after_permit_limit_per_ip()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var client = _fx.Factory!.CreateClient();

        var body = new LoginRequestModel
        {
            Email = "ratelimit@example.com",
            Password = "DefinitelyWrong1"
        };

        // First 5 attempts: 401 (bad credentials, but allowed by limiter).
        for (var i = 1; i <= 5; i++)
        {
            var r = await client.PostAsJsonAsync("/api/auth/login", body);
            r.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                because: $"attempt {i} should be permitted then fail auth");
        }

        // 6th attempt: rejected by the per-IP limiter before auth runs.
        var rateLimited = await client.PostAsJsonAsync("/api/auth/login", body);
        rateLimited.StatusCode.Should().Be((HttpStatusCode)429);
    }
}
