using WebApp.DbContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using WebApp.Services.Interface;
using WebApp.Services.Repository;
using WebApp.Services;
using Microsoft.AspNetCore.HttpOverrides;
using WebApp.Models.DatabaseModels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<MongoDbContext>();

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

builder.Services.AddHttpClient<GeolocationService>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<UserActivityLog>();
});

builder.Services.AddScoped<IUserGeolocationRepository, UserGeolocationRepository>();
builder.Services.AddScoped<IUserLogsRepository, UserLogsRepository>();
builder.Services.AddScoped<IUserGeolocationRepository, UserGeolocationRepository>();
builder.Services.AddScoped<IUserLogsRepository, UserLogsRepository>();
builder.Services.AddScoped<EmailService>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
