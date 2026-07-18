using System.Diagnostics;

// RequestLoggingMiddlewareは、HTTPリクエストの結果を構造化ログへ記録します。
// RequestIdMiddlewareの後に実行することで、同じRequest IDをログへ含めます。
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // リクエスト本文や認証ヘッダーは記録せず、調査に必要な情報だけを残します。
            _logger.LogInformation(
                "HTTP {HttpMethod} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}
