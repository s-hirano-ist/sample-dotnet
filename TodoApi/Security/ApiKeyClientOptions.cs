// ApiKeyClientOptionsは、1つのAPIクライアントの認証情報と権限です。
public sealed class ApiKeyClientOptions
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = Array.Empty<string>();
}
