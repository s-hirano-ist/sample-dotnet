// IdempotencyOptionsは、冪等性キーの記録を保持する時間を設定します。
public sealed class IdempotencyOptions
{
    public string Store { get; set; } = "Memory";
    public int EntryLifetimeSeconds { get; set; } = 300;
    public int WaitTimeoutSeconds { get; set; } = 5;
}
