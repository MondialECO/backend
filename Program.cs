using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApp.DbContext;
using WebApp.Models.DatabaseModels;
using WebApp.Services;
using WebApp.Services.Interface;
using WebApp.Services.Repository;


var builder = WebApplication.CreateBuilder(args);

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


// Repositories
builder.Services.AddScoped<BusinessIdeasRepository>();
builder.Services.AddScoped<InvestmentsRepository>();
builder.Services.AddScoped<TransactionsRepository>();
// Chat Repositories
builder.Services.AddScoped<MessagesRepository>();
builder.Services.AddScoped<ConversationRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<PushSubscriptionEntityRepository>();
// Services
builder.Services.AddScoped<IBusinessIdeasService, BusinessIdeasService>();
builder.Services.AddScoped<IInvestmentsService, InvestmentsService>();
builder.Services.AddScoped<ITransactionsService, TransactionsService>();


// Chat Service and Notification Service
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPushSubscriptionEntity, PushSubscriptionEntityService>();

builder.Services.AddScoped<ISubmmitdata, SubmmitdataRepository>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// signalR for real-time features
builder.Services.AddSignalR();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

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

app.Run();
