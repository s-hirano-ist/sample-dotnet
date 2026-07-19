// TodoIdempotencyDefaultsは、冪等性キーで使うHTTPヘッダー名をまとめます。
public static class TodoIdempotencyDefaults
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayHeaderName = "Idempotency-Replayed";
    public const int MaxKeyLength = 255;
}
