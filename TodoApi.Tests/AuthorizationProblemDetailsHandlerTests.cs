using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace TodoApi.Tests;

public class AuthorizationProblemDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenForbidden_ReturnsProblemDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var handler = new AuthorizationProblemDetailsHandler();

        // PolicyAuthorizationResult.Forbidは、認証済みだが権限不足の状態を表します。
        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            policy,
            PolicyAuthorizationResult.Forbid()
        );

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var body = await JsonNode.ParseAsync(context.Response.Body);
        Assert.NotNull(body);
        Assert.Equal("You do not have permission to perform this operation.", body["title"]?.GetValue<string>());
        Assert.Equal(403, body["status"]?.GetValue<int>());
    }
}
