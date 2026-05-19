using StackExchange.Redis;

namespace WebApp.Hubs
{
    /// <summary>
    /// Tracks which users have at least one live SignalR connection.
    /// Backed by Redis so presence is shared across every app replica
    /// (the old static Dictionary only worked in a single process and
    /// silently broke horizontal scaling).
    /// </summary>
    public interface IPresenceTracker
    {
        Task UserConnectedAsync(string userId, string connectionId);
        Task UserDisconnectedAsync(string userId, string connectionId);
        Task<bool> IsOnlineAsync(string userId);
    }

    public class RedisPresenceTracker : IPresenceTracker
    {
        private readonly IConnectionMultiplexer _redis;

        // Safety net: if a replica dies without firing OnDisconnected, the
        // presence key self-expires instead of marking a user online forever.
        private static readonly TimeSpan PresenceTtl = TimeSpan.FromHours(12);

        public RedisPresenceTracker(IConnectionMultiplexer redis) => _redis = redis;

        private static string Key(string userId) => $"presence:{userId}";

        public async Task UserConnectedAsync(string userId, string connectionId)
        {
            var db = _redis.GetDatabase();
            var key = Key(userId);
            await db.SetAddAsync(key, connectionId);
            await db.KeyExpireAsync(key, PresenceTtl);
        }

        public async Task UserDisconnectedAsync(string userId, string connectionId)
        {
            var db = _redis.GetDatabase();
            var key = Key(userId);
            await db.SetRemoveAsync(key, connectionId);
            if (await db.SetLengthAsync(key) == 0)
                await db.KeyDeleteAsync(key);
        }

        public async Task<bool> IsOnlineAsync(string userId)
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(Key(userId));
        }
    }
}
