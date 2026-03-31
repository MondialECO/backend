using Twilio;

namespace WebApp.Services
{
    public class TwilioService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;

        public TwilioService(IConfiguration config)
        {
            _accountSid = config["Twilio:AccountSid"];
            _authToken = config["Twilio:AuthToken"];
            _fromNumber = config["Twilio:FromNumber"];
            TwilioClient.Init(_accountSid, _authToken);
        }

        public async Task SendSmsAsync(string to, string message)
        {
            var msg = await MessageResource.CreateAsync(
                to: to,
                from: new Twilio.Types.PhoneNumber(_fromNumber),
                body: message
            );
        }
    }
}
