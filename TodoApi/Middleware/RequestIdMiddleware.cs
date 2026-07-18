// RequestIdMiddlewareは、リクエストを追跡するためのIDを作ります。
// 同じIDをレスポンスヘッダーとログへ入れることで、1回の通信を追跡しやすくします。
public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(
        RequestDelegate next,
        ILogger<RequestIdMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = GetRequestId(context.Request.Headers["X-Request-Id"]);

        // クライアントが送ったIDをそのまま受け入れず、形式を確認します。
        context.Response.Headers["X-Request-Id"] = requestId;

        // BeginScopeの中で出力されたログには、RequestIdが自動的に追加されます。
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId
        }))
        {
            await _next(context);
        }
    }

    private static string GetRequestId(string? headerValue)
    {
        return Guid.TryParse(headerValue, out var requestId)
            ? requestId.ToString("N")
            : Guid.NewGuid().ToString("N");
    }
}
