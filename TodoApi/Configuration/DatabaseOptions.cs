// DatabaseOptionsは、利用するDBプロバイダーを設定から受け取ります。
public sealed class DatabaseOptions
{
    public string Provider { get; set; } = "Sqlite";
    public bool ApplyMigrations { get; set; }
}
