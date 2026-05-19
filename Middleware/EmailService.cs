using WebApp.Services.Email;

/// <summary>
/// Public email API used by controllers. Now enqueues the message and
/// returns immediately; the actual SMTP send happens in
/// <see cref="EmailBackgroundService"/> off the request path. The bool
/// return now means "accepted for delivery" rather than "delivered".
/// Signature kept unchanged so callers need no modification.
/// </summary>
public class EmailService
{
    private readonly IEmailQueue _queue;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IEmailQueue queue, ILogger<EmailService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("SendEmailAsync called with empty recipient; skipped");
            return false;
        }

        await _queue.EnqueueAsync(new EmailMessage(toEmail, subject, body));
        _logger.LogInformation("Email to {To} queued for delivery", toEmail);
        return true;
    }
}
