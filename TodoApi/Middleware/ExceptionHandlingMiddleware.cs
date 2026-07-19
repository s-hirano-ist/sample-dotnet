using System.Text.Json;

// ExceptionHandlingMiddlewareは、処理中に予期しない例外が発生したときの
// HTTPレスポンスを共通化します。
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // 次のミドルウェアやエンドポイントを実行します。
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // クライアント切断によるキャンセルは、サーバー内部エラーではありません。
            // ASP.NET Coreへ返して、切断済みのレスポンスを書き込まないようにします。
            _logger.LogDebug("The request was canceled by the client.");
            throw;
        }
        catch (Exception exception)
        {
            // 詳細な例外情報はログにだけ残し、クライアントには返しません。
            _logger.LogError(exception, "Unhandled exception while processing the request.");

            // すでにレスポンスの送信が始まっている場合、JSONへ変更できません。
            if (context.Response.HasStarted)
            {
                throw;
            }

            // Response.Clear()でヘッダーも消えるため、RequestIdを先に退避します。
            var requestId = context.Response.Headers["X-Request-Id"].ToString();
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers["X-Request-Id"] = requestId;

            // ProblemDetailsは、HTTP APIのエラーを共通形式で表すためのJSONです。
            // RequestIdを含めると、利用者がログの該当行を運用担当者へ伝えられます。
            var problemDetails = new
            {
                type = "https://httpstatuses.com/500",
                title = "An unexpected error occurred.",
                status = StatusCodes.Status500InternalServerError,
                instance = context.Request.Path.Value,
                requestId
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, JsonSerializerOptions.Web);
        }
    }
}
