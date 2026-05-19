using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApp.Configuration;
using WebApp.DbContext;
using WebApp.HealthChecks;
using WebApp.Hubs;
using WebApp.Middleware;
using WebApp.Models.DatabaseModels;
using WebApp.Services;
using WebApp.Services.Interface;
using WebApp.Services.Repository;


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

// MongoClient → Singleton (recommended by MongoDB)
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
// IMongoDatabase → Singleton
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});
builder.Services.AddSingleton<MongoDbContext>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        //policy.AllowAnyOrigin()
        //      .AllowAnyMethod()
        //      .AllowAnyHeader();
        policy.WithOrigins(
        "http://localhost:3000",
        "https://mondialbusiness.eu" // Replace with actual domain
        )
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
        NameClaimType = JwtRegisteredClaimNames.Sub
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

// SignalR
builder.Services.AddSignalR();
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



// RadisCash
//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = "localhost:6379";
//});


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379"; // Redis server address
    options.InstanceName = "Mondial";       // Optional prefix
});



// need removed after using dashboard
builder.Services.AddScoped<ISubmmitdata, SubmmitdataRepository>();

// Email service
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<SaveFile>();
builder.Services.AddScoped<TwilioService>();

// Health checks: liveness (process up) is the bare endpoint; readiness
// (tagged "ready") verifies MongoDB + Redis so the orchestrator only routes
// traffic to replicas that can actually serve requests.
var redisConnection = builder.Configuration["Redis:Configuration"] ?? "localhost:6379";
builder.Services.AddHealthChecks()
    .AddCheck<MongoHealthCheck>(
        "mongodb",
        tags: new[] { "ready" })
    .AddRedis(
        redisConnection,
        name: "redis",
        tags: new[] { "ready" });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Trust the reverse proxy's forwarded headers first so client IP/scheme
// are correct for everything downstream (logging, rate limiting later).
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Correlation id must be established before the exception handler and
// request logging so both are tagged with it.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseHttpsRedirection();

// SignalR Hubs configuration
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");

app.MapControllers();

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
