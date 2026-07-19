// ApiKeyClientOptionsは、1つのAPIクライアントの認証情報と権限です。
public sealed class ApiKeyClientOptions
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = Array.Empty<string>();

    // ExpiresAtUtcを過ぎたキーは認証に使えません。nullなら期限なしです。
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    // 事故や退職などで即時に無効化したいキーに使います。
    public bool Revoked { get; set; }
}
