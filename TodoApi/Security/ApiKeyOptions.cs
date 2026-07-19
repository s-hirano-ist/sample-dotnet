// ApiKeyOptionsは、Authentication設定をC#の型として扱うためのクラスです。
public sealed class ApiKeyOptions
{
    // appsettings.jsonやUser SecretsのAuthentication:ApiKeyがここへ入ります。
    public string ApiKey { get; set; } = string.Empty;
}
