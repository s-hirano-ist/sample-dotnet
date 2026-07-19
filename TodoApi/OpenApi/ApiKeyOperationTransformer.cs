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
        // 現在のAPIでは、Todoの作成・更新・削除だけが認証必須です。
        if (context.Description.HttpMethod is "POST" or "PUT" or "DELETE")
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
