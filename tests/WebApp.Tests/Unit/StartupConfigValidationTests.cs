using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using WebApp.Configuration;
using Xunit;

namespace WebApp.Tests.Unit;

public class StartupConfigValidationTests
{
    private static WebApplicationBuilder BuilderWith(Dictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);
        return builder;
    }

    private static Dictionary<string, string?> ValidConfig() => new()
    {
        ["MongoDbSettings:ConnectionString"] = "mongodb://localhost:27017",
        ["MongoDbSettings:DatabaseName"] = "Test",
        ["JwtSettings:Issuer"] = "issuer",
        ["JwtSettings:Audience"] = "aud",
        ["JwtSettings:Key"] = new string('k', 32),
        ["EmailSettings:SmtpServer"] = "smtp.test",
        ["EmailSettings:Email"] = "a@b.com",
        ["EmailSettings:Password"] = "pw",
    };

    [Fact]
    public void Passes_with_complete_config()
    {
        var builder = BuilderWith(ValidConfig());
        var act = () => builder.ValidateRequiredConfiguration();
        act.Should().NotThrow();
    }

    [Fact]
    public void Throws_when_secret_missing()
    {
        var cfg = ValidConfig();
        cfg["MongoDbSettings:ConnectionString"] = "";
        var builder = BuilderWith(cfg);

        var act = () => builder.ValidateRequiredConfiguration();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*MongoDbSettings:ConnectionString*");
    }

    [Fact]
    public void Throws_when_jwt_key_too_weak()
    {
        var cfg = ValidConfig();
        cfg["JwtSettings:Key"] = "tooshort";
        var builder = BuilderWith(cfg);

        var act = () => builder.ValidateRequiredConfiguration();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*256-bit*");
    }
}
