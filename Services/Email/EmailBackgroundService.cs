using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WebApp.Services.Email
{
    /// <summary>
    /// Drains the email queue and performs the SMTP send with bounded
    /// exponential-backoff retry. Runs as a hosted background service so a
    /// slow or temporarily-failing SMTP server never blocks an HTTP request.
    /// </summary>
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IEmailQueue _queue;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailBackgroundService> _logger;

        private const int MaxAttempts = 4;

        public EmailBackgroundService(
            IEmailQueue queue,
            IConfiguration configuration,
            ILogger<EmailBackgroundService> logger)
        {
            _queue = queue;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await SendWithRetryAsync(message, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Email to {To} permanently failed after {Attempts} attempts",
                        message.To, MaxAttempts);
                }
            }
        }

        private async Task SendWithRetryAsync(EmailMessage message, CancellationToken ct)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    await SendSmtpAsync(message, ct);
                    _logger.LogInformation(
                        "Email sent to {To} (subject: {Subject})", message.To, message.Subject);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(4, attempt)); // 4s, 16s, 64s
                    _logger.LogWarning(ex,
                        "Email to {To} failed (attempt {Attempt}/{Max}); retrying in {Delay}s",
                        message.To, attempt, MaxAttempts, delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                }
            }

            // Final attempt outside the catch so a last failure propagates.
            await SendSmtpAsync(message, ct);
            _logger.LogInformation("Email sent to {To} on final attempt", message.To);
        }

        private async Task SendSmtpAsync(EmailMessage message, CancellationToken ct)
        {
            var s = _configuration.GetSection("EmailSettings");

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress("Mondial Eco", s["Email"]));
            mime.To.Add(new MailboxAddress("", message.To));
            mime.Subject = message.Subject;
            mime.Body = new TextPart("html") { Text = message.Body };

            using var client = new SmtpClient();
            await client.ConnectAsync(s["SmtpServer"], int.Parse(s["Port"]!),
                SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(s["Email"], s["Password"], ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}
