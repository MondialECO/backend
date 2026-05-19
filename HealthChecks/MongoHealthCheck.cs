using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WebApp.HealthChecks
{
    /// <summary>
    /// Readiness check for MongoDB. Reuses the application's already-registered
    /// IMongoDatabase (MongoDB.Driver) and issues a lightweight { ping: 1 }
    /// command. Hand-rolled to avoid the AspNetCore.HealthChecks.MongoDb
    /// package's hard binding to an older driver version.
    /// </summary>
    public class MongoHealthCheck : IHealthCheck
    {
        private readonly IMongoDatabase _database;

        public MongoHealthCheck(IMongoDatabase database) => _database = database;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _database.RunCommandAsync<BsonDocument>(
                    new BsonDocument("ping", 1), cancellationToken: cancellationToken);
                return HealthCheckResult.Healthy("MongoDB reachable");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("MongoDB unreachable", ex);
            }
        }
    }
}
