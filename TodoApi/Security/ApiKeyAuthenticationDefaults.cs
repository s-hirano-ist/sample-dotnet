// ApiKeyAuthenticationDefaultsは、APIキー認証で共有する名前をまとめます。
// 認証方式名を文字列として複数箇所に直接書かないためのクラスです。
public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";
}
