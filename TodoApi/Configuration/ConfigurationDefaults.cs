// ConfigurationDefaultsは、設定ファイルで共有するセクション名をまとめます。
public static class ConfigurationDefaults
{
    public const string AuthenticationSection = "Authentication";
    public const string CorsSection = "Cors";
    public const string RateLimitSection = "RateLimit";
    public const string IdempotencySection = "Idempotency";
    public const string TodoDatabaseConnection = "TodoDatabase";
    public const string RedisConnection = "Redis";
}
