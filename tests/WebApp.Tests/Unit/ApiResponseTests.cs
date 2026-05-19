using FluentAssertions;
using WebApp.Models;
using Xunit;

namespace WebApp.Tests.Unit;

public class ApiResponseTests
{
    [Fact]
    public void Ok_sets_success_and_payload()
    {
        var data = new { id = 1 };
        var r = ApiResponse.Ok("done", data);

        r.Success.Should().BeTrue();
        r.Message.Should().Be("done");
        r.Data.Should().Be(data);
        r.TraceId.Should().BeNull();
    }

    [Fact]
    public void Error_sets_failure_and_trace_id()
    {
        var r = ApiResponse.Error("nope", "trace-123");

        r.Success.Should().BeFalse();
        r.Message.Should().Be("nope");
        r.TraceId.Should().Be("trace-123");
    }
}
