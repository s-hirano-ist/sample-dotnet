using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

// ApiKeyOperationTransformerは、認証が必要な操作へAPIキー要求を追加します。
// 実際のエンドポイントに付けたRequireAuthorizationとOpenAPI仕様を一致させます。
public sealed class ApiKeyOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        // RequireAuthorizationが追加した認証メタデータを確認します。
        // HTTPメソッド名を直接判定しないため、認証ルールの変更に追従できます。
        var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (requiresAuthorization)
        {
            operation.Security ??= [];
            operation.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("ApiKey")] = []
                }
            );
        }

        return Task.CompletedTask;
    }
}
