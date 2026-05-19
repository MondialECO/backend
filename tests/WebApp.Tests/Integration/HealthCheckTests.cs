using System.Net;
using FluentAssertions;
using Xunit;

namespace WebApp.Tests.Integration;

public class HealthCheckTests : IClassFixture<AppFixture>
{
    private readonly AppFixture _fx;

    public HealthCheckTests(AppFixture fx) => _fx = fx;

    [SkippableFact]
    public async Task Liveness_endpoint_returns_200()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);

        var client = _fx.Factory!.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Readiness_endpoint_reports_healthy_with_mongo_and_redis()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);

        var client = _fx.Factory!.CreateClient();
        var response = await client.GetAsync("/health/ready");

        // Mongo + Redis containers are up, so readiness should pass.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Unknown_route_returns_404_not_a_stack_trace()
    {
        Skip.IfNot(_fx.Available, _fx.SkipReason);

        var client = _fx.Factory!.CreateClient();
        var response = await client.GetAsync("/definitely/not/a/route");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
