using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WebApp.Observability
{
    /// <summary>
    /// Configures OpenTelemetry traces + metrics. Metrics are always
    /// exposed at /metrics for Prometheus scraping. Traces/metrics are
    /// additionally exported via OTLP only when an endpoint is configured
    /// (OpenTelemetry:OtlpEndpoint or the standard OTEL_EXPORTER_OTLP_ENDPOINT
    /// env var) so there are no noisy connection errors without a collector.
    /// </summary>
    public static class ObservabilitySetup
    {
        public const string ServiceName = "MondialBackend";

        public static void AddObservability(this WebApplicationBuilder builder)
        {
            var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            var hasOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

            var resource = ResourceBuilder.CreateDefault()
                .AddService(ServiceName,
                    serviceVersion: typeof(ObservabilitySetup).Assembly.GetName().Version?.ToString());

            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(resource)
                        .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                        .AddHttpClientInstrumentation();
                    if (hasOtlp) tracing.AddOtlpExporter();
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(resource)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddPrometheusExporter();
                    if (hasOtlp) metrics.AddOtlpExporter();
                });
        }
    }
}
