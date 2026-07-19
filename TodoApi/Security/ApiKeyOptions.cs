// ApiKeyOptionsは、Authentication設定をC#の型として扱うためのクラスです。
public sealed class ApiKeyOptions
{
    // appsettings.jsonやUser SecretsのAuthentication:ApiKeyがここへ入ります。
    public string ApiKey { get; set; } = string.Empty;

    // 認証成功時に付与する権限です。既定ではTodoを書き込めます。
    public string[] Permissions { get; set; } = Array.Empty<string>();
}
