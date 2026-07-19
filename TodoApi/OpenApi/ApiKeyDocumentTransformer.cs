using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

// ApiKeyDocumentTransformerは、実際の認証方式をOpenAPI仕様へ追加します。
// これによりSwagger UIにX-API-Key用のAuthorize入力欄が表示されます。
public sealed class ApiKeyDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[ApiKeyAuthenticationDefaults.AuthenticationScheme] =
            new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = ApiKeyAuthenticationDefaults.HeaderName,
            In = ParameterLocation.Header,
            Description = "API key used for protected Todo operations."
        };

        return Task.CompletedTask;
    }
}
