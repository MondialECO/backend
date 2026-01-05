namespace WebApp.Hubs
{
    public static class PresenceTracker
    {
        private static readonly Dictionary<string, HashSet<string>> OnlineUsers = new();

        public static void UserConnected(string userId, string connectionId)
        {
            lock (OnlineUsers)
            {
                if (!OnlineUsers.ContainsKey(userId))
                    OnlineUsers[userId] = new HashSet<string>();

                OnlineUsers[userId].Add(connectionId);
            }
        }

        public static void UserDisconnected(string userId, string connectionId)
        {
            lock (OnlineUsers)
            {
                if (!OnlineUsers.ContainsKey(userId)) return;

                OnlineUsers[userId].Remove(connectionId);
                if (OnlineUsers[userId].Count == 0)
                    OnlineUsers.Remove(userId);
            }
        }

        public static bool IsOnline(string userId) => OnlineUsers.ContainsKey(userId);
    }

}
