using Serilog.Context;

namespace WebApp.Middleware
{
    /// <summary>
    /// Ensures every request has a correlation id. Reads an inbound
    /// X-Correlation-ID header if the caller (or reverse proxy) supplies one,
    /// otherwise generates one. The id is echoed back on the response and
    /// pushed into the Serilog LogContext so every log line for the request
    /// is tagged with it. Essential for tracing issues across replicas.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
                && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items[HeaderName] = correlationId;
            context.TraceIdentifier = correlationId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}
