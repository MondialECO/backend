using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApp.Configuration;
using WebApp.DbContext;
using WebApp.Filters;
using WebApp.HealthChecks;
using WebApp.Hubs;
using WebApp.Middleware;
using WebApp.Models;
using WebApp.Models.DatabaseModels;
using WebApp.Observability;
using WebApp.Services.Audit;
using WebApp.Services;
using WebApp.Services.Email;
using WebApp.Services.Interface;
using WebApp.Services.Repository;
using WebApp.Validation;


// Bootstrap logger: captures errors that occur during startup itself
// (before the full Serilog pipeline is configured).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Refuse to start if required secrets/config are missing or weak.
builder.ValidateRequiredConfiguration();

// Structured logging for every request, enriched with the correlation id.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MondialBackend")
    .WriteTo.Console());

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// MongoClient → Singleton (recommended by MongoDB). Production-tuned:
// bounded server-selection/connect timeouts so a DB blip fails fast
// instead of blocking request threads, an explicit connection pool so
// spiky traffic cannot exhaust connections, and retryable reads/writes
// so transient primary elections recover transparently.
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
    clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
    clientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
    clientSettings.SocketTimeout = TimeSpan.FromSeconds(30);
    clientSettings.MaxConnectionPoolSize = 200;
    clientSettings.MinConnectionPoolSize = 10;
    clientSettings.RetryReads = true;
    clientSettings.RetryWrites = true;
    return new MongoClient(clientSettings);
});
// IMongoDatabase → Singleton
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});
builder.Services.AddSingleton<MongoDbContext>();

// ---- Shared Redis (required for multi-replica/stateless operation) ----
// One multiplexer is reused for caching, the SignalR backplane, presence,
// and the DataProtection key ring so every replica shares the same state.
var redisConnection = builder.Configuration["Redis:Configuration"] ?? "localhost:6379";
var redisInstanceName = builder.Configuration["Redis:InstanceName"] ?? "Mondial";

// AbortOnConnectFail=false: do not crash-loop if Redis is briefly
// unreachable at startup; the multiplexer reconnects in the background
// and the Redis readiness health check keeps this replica out of the
// load balancer until Redis is actually available.
var redisOptions = ConfigurationOptions.Parse(redisConnection);
redisOptions.AbortOnConnectFail = false;
redisOptions.ConnectRetry = 5;
redisOptions.ConnectTimeout = 5000;
redisOptions.KeepAlive = 60;
var redisMultiplexer = ConnectionMultiplexer.Connect(redisOptions);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);
builder.Services.AddSingleton<IPresenceTracker, RedisPresenceTracker>();

// Shared DataProtection key ring: without this each replica generates its
// own keys, so auth/reset tokens and antiforgery break behind a load
// balancer. SetApplicationName must be identical across replicas.
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(redisMultiplexer, $"{redisInstanceName}-DataProtection-Keys")
    .SetApplicationName("MondialBackend");


// Allowed origins come from configuration (Cors:AllowedOrigins) so each
// environment sets its own without a redeploy. Credentials are allowed,
// so origins must be explicit (wildcard + credentials is invalid anyway).
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "https://mondialbusiness.eu" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(identityOptions =>
{
    identityOptions.Password.RequireDigit = true;
    identityOptions.Password.RequiredLength = 6;
    identityOptions.Password.RequireNonAlphanumeric = false;
    identityOptions.Password.RequireUppercase = true;
    identityOptions.Password.RequireLowercase = true;
    identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    identityOptions.Lockout.MaxFailedAccessAttempts = 5;
    identityOptions.Lockout.AllowedForNewUsers = true;
    identityOptions.User.RequireUniqueEmail = true;
})
.AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>(
    builder.Configuration["MongoDbSettings:ConnectionString"],
    builder.Configuration["MongoDbSettings:DatabaseName"])
.AddDefaultTokenProviders();

// Add Authentication using JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        // Default ClockSkew is 5 min: a revoked/expired token would
        // remain valid for that long. 30s is enough for NTP drift.
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    // SignalR access token support
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// SignalR with a Redis backplane so connections/messages are shared
// across all replicas (a client connected to replica A still receives
// messages published from replica B).
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, o =>
    {
        o.Configuration.ChannelPrefix = RedisChannel.Literal($"{redisInstanceName}SignalR");
    });
// Define CustomUserIdProvider for SignalR
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// Busess ideas, investments, transactions services and repositories
builder.Services.AddScoped<IBusinessIdeasService, BusinessIdeasService>();
builder.Services.AddScoped<BusinessIdeasRepository>();

// Investments
builder.Services.AddScoped<IInvestmentsService, InvestmentsService>();
builder.Services.AddScoped<InvestmentsRepository>();

// Transactions
builder.Services.AddScoped<ITransactionsService, TransactionsService>();
builder.Services.AddScoped<TransactionsRepository>();

// Web Push service and repositories
builder.Services.AddScoped<IPushSubscriptionEntity, PushSubscriptionEntityService>();
builder.Services.AddScoped<PushSubscriptionEntityRepository>();

// Chat services and repositories
builder.Services.AddScoped<MessagesRepository>();
builder.Services.AddScoped<ConversationRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
// Notification services and repositories
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<NotificationRepository>();
// Web Push service
builder.Services.AddScoped<WebPushService>();



// Distributed cache on the same shared Redis (connection from config).
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = redisInstanceName;
});



// need removed after using dashboard
builder.Services.AddScoped<ISubmmitdata, SubmmitdataRepository>();

// Observability: OpenTelemetry traces + metrics (/metrics for Prometheus).
builder.AddObservability();

// Audit trail for security-sensitive operations.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();

// Email: queue (singleton) + background sender; EmailService now enqueues.
builder.Services.AddSingleton<IEmailQueue, EmailQueue>();
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<SaveFile>();
builder.Services.AddScoped<TwilioService>();

// Health checks: liveness (process up) is the bare endpoint; readiness
// (tagged "ready") verifies MongoDB + Redis so the orchestrator only routes
// traffic to replicas that can actually serve requests.
builder.Services.AddHealthChecks()
    .AddCheck<MongoHealthCheck>(
        "mongodb",
        tags: new[] { "ready" })
    .AddRedis(
        redisConnection,
        name: "redis",
        tags: new[] { "ready" });

// FluentValidation: validators run via ValidationFilter, returning the
// shared ApiResponse envelope on failure.
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestModelValidator>();

// Rate limiting. Global per-IP limit protects every endpoint; the stricter
// "auth" policy is applied to AuthController to blunt credential brute-force.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        var payload = ApiResponse.Error(
            "Too many requests. Please slow down and try again shortly.",
            ctx.HttpContext.TraceIdentifier);
        await ctx.HttpContext.Response.WriteAsJsonAsync(payload, token);
    };

    // Per-IP partition: a brute-force attempt from one IP must NOT lock
    // other legitimate users out of /login. 5 attempts/min/IP.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// API versioning. Non-breaking: a default version is assumed when the
// client does not specify one, so existing routes/clients keep working.
// Clients can opt into versions via the X-Api-Version header or
// ?api-version= query string.
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
});

builder.Services.AddAuthorization();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    // Make the built-in [ApiController] model-state 400 use the shared
    // ApiResponse envelope so every validation error has one shape.
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var payload = WebApp.Models.ApiResponse.Error(
            "Validation failed.",
            context.HttpContext.TraceIdentifier,
            errors);
        return new BadRequestObjectResult(payload);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Response compression: JSON APIs compress 3-8x, lowering bandwidth and
// TTFB on slow/mobile links. EnableForHttps=true is standard for JSON
// SaaS APIs - the BREACH/CRIME class of attacks targets responses that
// mix attacker-controlled input alongside secrets in one body, which
// this API does not do.
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/problem+json"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// Kestrel limits: cap body size, header size and connection count so a
// hostile or runaway client cannot exhaust memory/sockets under spiky
// traffic. Body size is configurable (uploads); the rest are safe caps.
var maxBodyBytes = builder.Configuration.GetValue<long?>("Limits:MaxRequestBodyBytes")
    ?? 10 * 1024 * 1024; // 10 MB default
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = maxBodyBytes;
    o.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
    o.Limits.MaxConcurrentConnections = 1000;
    o.Limits.MaxConcurrentUpgradedConnections = 1000;
    o.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(120);
    o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Request timeouts: a hung/slow handler frees resources (504) instead of
// pinning a request thread indefinitely and cascading under load.
// SignalR hub connections are long-lived and opt out explicitly below.
builder.Services.AddRequestTimeouts(o =>
{
    o.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30),
        TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
    };
});

var app = builder.Build();

// Trust forwarded headers ONLY from known proxies/networks. Otherwise a
// client could spoof X-Forwarded-For to evade the per-IP rate limiter and
// poison audit logs. Defaults cover loopback + RFC1918/Docker ranges
// (Traefik sits on the private network); override via config per env.
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
fwdOptions.KnownNetworks.Clear();
fwdOptions.KnownProxies.Clear();

var knownNetworks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>()
    ?? new[] { "127.0.0.0/8", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "::1/128" };
foreach (var cidr in knownNetworks)
{
    var parts = cidr.Split('/');
    if (parts.Length == 2 &&
        System.Net.IPAddress.TryParse(parts[0], out var prefix) &&
        int.TryParse(parts[1], out var prefixLength))
    {
        fwdOptions.KnownNetworks.Add(
            new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
    }
}

foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>()
    ?? Array.Empty<string>())
{
    if (System.Net.IPAddress.TryParse(proxy, out var ip))
        fwdOptions.KnownProxies.Add(ip);
}

app.UseForwardedHeaders(fwdOptions);

// Correlation id must be established before the exception handler and
// request logging so both are tagged with it.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestTimeouts();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// SignalR Hubs: long-lived connections must opt out of the request
// timeout or they would be killed after 30s.
app.MapHub<NotificationHub>("/hubs/notifications").DisableRequestTimeout();
app.MapHub<ChatHub>("/hubs/chat").DisableRequestTimeout();

app.MapControllers();

// Prometheus metrics scrape endpoint. Restrict this at the reverse proxy
// (Phase 8) so it is not publicly exposed.
app.MapPrometheusScrapingEndpoint();

// RFC 9116 vulnerability disclosure. Served as a route (not a static
// file) because ASP.NET Core's static file provider excludes dotted
// directories by default. Update Expires before it lapses (max ~1 year).
app.MapGet("/.well-known/security.txt", () => Results.Text(
    "Contact: mailto:security@mondialbusiness.eu\n" +
    "Expires: 2027-05-20T00:00:00Z\n" +
    "Preferred-Languages: en\n",
    "text/plain"));

// Build/version reporting for incident triage: which replica/sha is
// actually serving? Image build threads BUILD_SHA / BUILD_TIME via
// docker build args.
var asm = typeof(Program).Assembly;
var versionInfo = new
{
    service = "MondialBackend",
    version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
              ?? asm.GetName().Version?.ToString()
              ?? "unknown",
    commit = Environment.GetEnvironmentVariable("BUILD_SHA") ?? "local",
    buildTime = Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown",
    environment = app.Environment.EnvironmentName
};
app.MapGet("/version", () => Results.Json(versionInfo));

// Liveness: process is up and the pipeline responds (no dependency checks).
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
// Readiness: MongoDB + Redis reachable. Used by the orchestrator/reverse
// proxy to decide whether this replica should receive traffic.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

try
{
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Exposed so WebApplicationFactory<Program> can bootstrap the app in
// integration tests (top-level statements otherwise emit an internal Program).
public partial class Program { }
