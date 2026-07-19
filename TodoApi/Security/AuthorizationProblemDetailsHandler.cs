using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

// AuthorizationProblemDetailsHandlerは、認証済みだが権限不足の403をJSON化します。
public sealed class AuthorizationProblemDetailsHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult
    )
    {
        if (authorizeResult.Forbidden && !context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new
            {
                type = "https://httpstatuses.com/403",
                title = "You do not have permission to perform this operation.",
                status = StatusCodes.Status403Forbidden
            };

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                problemDetails,
                JsonSerializerOptions.Web
            );
            return;
        }

        // 401や認可成功時は、ASP.NET Core標準の処理へ委譲します。
        var defaultHandler = new AuthorizationMiddlewareResultHandler();
        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
