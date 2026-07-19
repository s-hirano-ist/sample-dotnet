using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// HealthCheckResponseWriterは、監視用に安全で小さなJSONを返します。
// DB接続文字列や例外の詳細など、外部へ不要な情報は返しません。
public static class HealthCheckResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString()
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonSerializerOptions.Web);
    }
}
