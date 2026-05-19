namespace WebApp.Middleware
{
    /// <summary>
    /// Adds baseline security response headers. This is a JSON API (plus
    /// Swagger in Development), so the CSP is locked down to 'none' by
    /// default; the Swagger UI paths get a relaxed policy so the docs page
    /// still renders in Development.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

        public Task InvokeAsync(HttpContext context)
        {
            var isSwagger = context.Request.Path.StartsWithSegments("/swagger");

            // Set on OnStarting so the headers survive the exception
            // handler's Response.Clear() and apply to error responses too.
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
                headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
                headers.Remove("Server");
                headers.Remove("X-Powered-By");
                headers["Content-Security-Policy"] = isSwagger
                    ? "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; img-src 'self' data:"
                    : "default-src 'none'; frame-ancestors 'none'";
                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
