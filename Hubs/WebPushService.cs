using Microsoft.Extensions.Options;
using System.Text.Json;
using WebPush;
using WebApp.Models.Dtos;
using WebApp.Models.DatabaseModels;

namespace WebApp.Hubs
{
    public class WebPushService
    {
        private readonly WebPushClient _client;
        private readonly VapidDetails _vapid;

        public WebPushService(IOptions<VapidSettings> options)
        {
            var v = options.Value;
            _client = new WebPushClient();
            _vapid = new VapidDetails(v.Subject, v.PublicKey, v.PrivateKey);
        }

        public async Task SendAsync(PushSubscriptionEntity sub, object payload)
        {
            if (!sub.IsActive) return;

            var pushSub = new PushSubscription(
                sub.Endpoint,
                sub.P256dh,
                sub.Auth
            );

            var json = JsonSerializer.Serialize(payload);

            try
            {
                await _client.SendNotificationAsync(pushSub, json, _vapid);
            }
            catch (WebPushException ex)
            {
                // handle expired or invalid subscription
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    sub.IsActive = false;
                    // Update MongoDB to mark inactive
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
