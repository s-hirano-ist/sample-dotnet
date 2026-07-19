// SecurityHeadersMiddlewareは、ブラウザ向けの基本的なセキュリティヘッダーを追加します。
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // レスポンス本文の送信前にヘッダーを追加する必要があります。
        if (!context.Response.HasStarted)
        {
            // Content-Typeを推測して別の形式として解釈する動作を抑止します。
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            // APIをiframeへ埋め込まれにくくします。
            context.Response.Headers["X-Frame-Options"] = "DENY";
            // Refererから不要なURL情報を送らないようにします。
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
        }

        await _next(context);
    }
}
