using Microsoft.AspNetCore.Http;

namespace TodoApi.Tests;

public class TodoConcurrencyTests
{
    [Fact]
    public void MatchesIfMatch_WhenHeaderIsMissing_ReturnsTrue()
    {
        var context = new DefaultHttpContext();

        Assert.True(TodoConcurrency.MatchesIfMatch(context.Request, "\"etag\""));
    }

    [Fact]
    public void MatchesIfMatch_WhenHeaderMatches_ReturnsTrue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["If-Match"] = "\"etag\"";

        Assert.True(TodoConcurrency.MatchesIfMatch(context.Request, "\"etag\""));
    }

    [Fact]
    public void MatchesIfMatch_WhenHeaderIsStale_ReturnsFalse()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["If-Match"] = "\"old-etag\"";

        Assert.False(TodoConcurrency.MatchesIfMatch(context.Request, "\"current-etag\""));
    }
}
