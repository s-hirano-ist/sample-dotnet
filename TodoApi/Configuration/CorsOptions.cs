// CorsOptionsは、ブラウザからのアクセスを許可するOriginの設定です。
public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
