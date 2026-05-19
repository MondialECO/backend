using System.Text.Json;
using WebApp.Models;

namespace WebApp.Middleware
{
    /// <summary>
    /// Catches any unhandled exception, logs it with the request's correlation
    /// id, and returns a consistent JSON envelope instead of leaking a stack
    /// trace. Exception details are only included when the environment is
    /// Development.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = context.TraceIdentifier;
                _logger.LogError(ex,
                    "Unhandled exception for {Method} {Path} (CorrelationId: {CorrelationId})",
                    context.Request.Method, context.Request.Path, correlationId);

                if (context.Response.HasStarted)
                {
                    // Response already partially sent; can't rewrite it.
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var message = _env.IsDevelopment()
                    ? $"{ex.GetType().Name}: {ex.Message}"
                    : "An unexpected error occurred. Please try again later.";

                var payload = ApiResponse.Error(message, correlationId);
                await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
            }
        }
    }
}
