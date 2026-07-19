// RateLimitOptionsは、レート制限の設定をC#の型として扱うクラスです。
public sealed class RateLimitOptions
{
    public string Store { get; set; } = "Memory";
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 10;
}
