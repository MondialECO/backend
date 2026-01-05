using Microsoft.Extensions.Options;
using System.Text.Json;
using WebApp.Models.Dtos;
using WebPush;

namespace WebApp.Services
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

        //public async Task SendAsync(PushSubscriptionEntity sub, object payload)
        //{
        //    var pushSub = new PushSubscription(
        //        sub.Endpoint,
        //        sub.P256dh,
        //        sub.Auth
        //    );

        //    var json = JsonSerializer.Serialize(payload);

        //    await _client.SendNotificationAsync(pushSub, json, _vapid);
        //}
    }

    public class PushSubscriptionEntity
    {
    }
}
