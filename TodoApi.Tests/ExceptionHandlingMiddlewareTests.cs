using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace TodoApi.Tests;

// ExceptionHandlingMiddlewareはHTTPパイプラインの部品なので、
// DefaultHttpContextを使ってミドルウェア単体の動作も確認できます。
public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ReturnsProblemDetailsWithoutExceptionDetails()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/todos";
        context.Response.Body = new MemoryStream();
        context.Response.Headers["X-Request-Id"] = "request-123";

        // 例外を投げる処理を、次のミドルウェアとして用意します。
        RequestDelegate next = _ => throw new InvalidOperationException("secret database detail");
        var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var body = await JsonNode.ParseAsync(context.Response.Body);
        Assert.NotNull(body);
        Assert.Equal("An unexpected error occurred.", body["title"]?.GetValue<string>());
        Assert.Equal(500, body["status"]?.GetValue<int>());
        Assert.Equal("request-123", body["requestId"]?.GetValue<string>());

        // 例外のメッセージは、クライアントへ漏れていないことを確認します。
        Assert.DoesNotContain("secret database detail", body.ToJsonString());
    }

    [Fact]
    public async Task RequestIdMiddlewareBeforeExceptionMiddleware_PreservesRequestIdOnError()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/todos";
        context.Response.Body = new MemoryStream();

        RequestDelegate endpoint = _ => throw new InvalidOperationException("unexpected failure");
        var exceptionMiddleware = new ExceptionHandlingMiddleware(
            endpoint,
            NullLogger<ExceptionHandlingMiddleware>.Instance
        );
        var requestIdMiddleware = new RequestIdMiddleware(
            exceptionMiddleware.InvokeAsync,
            NullLogger<RequestIdMiddleware>.Instance
        );

        // Request ID Middlewareが外側にある構成を再現します。
        await requestIdMiddleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonNode.ParseAsync(context.Response.Body);
        Assert.NotNull(body);
        Assert.Equal(
            context.Response.Headers["X-Request-Id"].ToString(),
            body["requestId"]?.GetValue<string>()
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsCanceled_RethrowsCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var context = new DefaultHttpContext
        {
            RequestAborted = cancellationSource.Token
        };
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ =>
            throw new OperationCanceledException(cancellationSource.Token);
        var middleware = new ExceptionHandlingMiddleware(
            next,
            NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        // クライアント切断は500へ変換せず、キャンセルとして上位へ伝播させます。
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => middleware.InvokeAsync(context)
        );
        Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }
}
