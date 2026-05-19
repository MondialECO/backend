using System.Net;
using FluentAssertions;
using Xunit;

namespace WebApp.Tests.Integration;

/// <summary>
/// End-to-end checks that the security/observability middleware actually
/// fires on every response - guards against silently misconfigured
/// pipeline ordering or middleware regressions.
/// </summary>
public class MiddlewareIntegrationTests : IClassFixture<AppFixture>
{
    private readonly AppFixture _fx;

    public MiddlewareIntegrationTests(AppFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Security_headers_are_present_on_every_response()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var client = _fx.Factory!.CreateClient();

        var r = await client.GetAsync("/health/live");

        r.Headers.TryGetValues("X-Content-Type-Options", out var nosniff).Should().BeTrue();
        nosniff!.Should().Contain("nosniff");
        r.Headers.TryGetValues("X-Frame-Options", out var xfo).Should().BeTrue();
        xfo!.Should().Contain("DENY");
        r.Headers.TryGetValues("Content-Security-Policy", out _).Should().BeTrue();
        r.Headers.TryGetValues("Referrer-Policy", out _).Should().BeTrue();
    }

    [SkippableFact]
    public async Task Correlation_id_is_generated_when_caller_omits_it()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var client = _fx.Factory!.CreateClient();

        var r = await client.GetAsync("/health/live");

        r.Headers.TryGetValues("X-Correlation-ID", out var ids).Should().BeTrue();
        ids!.Single().Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task Correlation_id_is_echoed_back_when_caller_supplies_it()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var client = _fx.Factory!.CreateClient();
        var supplied = "test-corr-" + Guid.NewGuid().ToString("N");

        var req = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        req.Headers.Add("X-Correlation-ID", supplied);
        var r = await client.SendAsync(req);

        r.Headers.GetValues("X-Correlation-ID").Single().Should().Be(supplied);
    }

    [SkippableFact]
    public async Task Version_endpoint_reports_service_metadata()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var client = _fx.Factory!.CreateClient();

        var r = await client.GetAsync("/version");

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await r.Content.ReadAsStringAsync();
        body.Should().Contain("MondialBackend");
        body.Should().Contain("environment");
    }

    [SkippableFact]
    public async Task Security_txt_is_served_per_rfc_9116()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);
        var client = _fx.Factory!.CreateClient();

        var r = await client.GetAsync("/.well-known/security.txt");

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        r.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        var body = await r.Content.ReadAsStringAsync();
        body.Should().Contain("Contact:");
        body.Should().Contain("Expires:");
    }
}
