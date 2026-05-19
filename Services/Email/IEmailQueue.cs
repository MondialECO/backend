using System.Threading.Channels;

namespace WebApp.Services.Email
{
    /// <summary>
    /// In-process email queue. Requests enqueue and return immediately;
    /// a background worker drains the queue and performs the slow SMTP
    /// send off the request path.
    /// </summary>
    public interface IEmailQueue
    {
        ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);
        ChannelReader<EmailMessage> Reader { get; }
    }

    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<EmailMessage> _channel;

        public EmailQueue()
        {
            // Bounded with Wait so a flood applies backpressure to callers
            // instead of growing memory unbounded.
            _channel = Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default)
            => _channel.Writer.WriteAsync(message, ct);

        public ChannelReader<EmailMessage> Reader => _channel.Reader;
    }
}
