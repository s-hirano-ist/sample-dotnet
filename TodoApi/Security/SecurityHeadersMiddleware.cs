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
            // Todo APIが使わないブラウザ機能を無効にします。
            context.Response.Headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=()";

            // Swagger UIは画面表示のためにスクリプトとスタイルを使うため、CSPの対象外にします。
            // APIレスポンスでは外部リソースを読み込まないポリシーを返します。
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                context.Response.Headers["Content-Security-Policy"] =
                    "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }

            // HTTPS接続時だけ、以後もHTTPSを使うようブラウザへ伝えます。
            // HTTP接続へこのヘッダーを返してもブラウザは安全な移行として扱えません。
            if (context.Request.IsHttps)
            {
                context.Response.Headers["Strict-Transport-Security"] =
                    "max-age=31536000; includeSubDomains";
            }
        }

        await _next(context);
    }
}
