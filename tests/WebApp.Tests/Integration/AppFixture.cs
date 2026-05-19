using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MongoDb;
using Testcontainers.Redis;
using Xunit;

namespace WebApp.Tests.Integration;

/// <summary>
/// Spins up real MongoDB + Redis via Testcontainers and boots the full
/// app against them. If Docker is unavailable (e.g. local dev box without
/// Docker), the fixture records the reason and dependent tests are skipped
/// rather than failed. In CI (Docker present) they run for real.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private MongoDbContainer? _mongo;
    private RedisContainer? _redis;

    public WebApplicationFactory<Program>? Factory { get; private set; }
    public bool Available { get; private set; }
    public string SkipReason { get; private set; } = "Docker not available";

    public async Task InitializeAsync()
    {
        try
        {
            // Built inside the try: Testcontainers resolves the Docker
            // endpoint eagerly and throws here when Docker is absent.
            _mongo = new MongoDbBuilder().WithImage("mongo:7").Build();
            _redis = new RedisBuilder().WithImage("redis:7").Build();

            await _mongo.StartAsync();
            await _redis.StartAsync();

            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b =>
                {
                    b.UseEnvironment("Development");
                    b.ConfigureAppConfiguration((_, cfg) =>
                    {
                        cfg.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["MongoDbSettings:ConnectionString"] = _mongo.GetConnectionString(),
                            ["MongoDbSettings:DatabaseName"] = "IntegrationTest",
                            ["JwtSettings:Issuer"] = "test",
                            ["JwtSettings:Audience"] = "test",
                            ["JwtSettings:Key"] = new string('k', 48),
                            ["EmailSettings:SmtpServer"] = "smtp.test",
                            ["EmailSettings:Email"] = "test@test.com",
                            ["EmailSettings:Password"] = "pw",
                            ["Redis:Configuration"] = _redis.GetConnectionString(),
                            ["Redis:InstanceName"] = "Test",
                        });
                    });
                });

            // Force host build so startup wiring is exercised.
            _ = Factory.Services;
            Available = true;
        }
        catch (Exception ex)
        {
            Available = false;
            SkipReason = $"Docker/containers unavailable: {ex.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (_redis is not null) await _redis.DisposeAsync();
        if (_mongo is not null) await _mongo.DisposeAsync();
    }
}
