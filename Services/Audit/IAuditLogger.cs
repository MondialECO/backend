namespace WebApp.Services.Audit
{
    /// <summary>
    /// Structured audit trail for security-sensitive operations
    /// (authentication, credential changes, privileged/destructive actions).
    /// Emitted on a dedicated "Audit" log category with the request
    /// correlation id so events can be filtered and traced.
    /// </summary>
    public interface IAuditLogger
    {
        void Record(string action, string actor, bool success, object? details = null);
    }

    public class AuditLogger : IAuditLogger
    {
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _http;

        public AuditLogger(ILoggerFactory loggerFactory, IHttpContextAccessor http)
        {
            _logger = loggerFactory.CreateLogger("Audit");
            _http = http;
        }

        public void Record(string action, string actor, bool success, object? details = null)
        {
            var ctx = _http.HttpContext;
            _logger.LogInformation(
                "AUDIT {Action} actor={Actor} success={Success} ip={Ip} correlationId={CorrelationId} details={@Details}",
                action,
                actor,
                success,
                ctx?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                ctx?.TraceIdentifier ?? "none",
                details);
        }
    }
}
