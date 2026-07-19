// OpenTelemetryOptionsは、テレメトリの出力先と有効・無効を設定します。
public sealed class OpenTelemetryOptions
{
    public bool Enabled { get; set; }
    public string OtlpEndpoint { get; set; } = string.Empty;
}
