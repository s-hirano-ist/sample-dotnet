// IIdempotencyStoreは、冪等性キーの保存先を隠すインターフェースです。
// インメモリとRedisを同じAPI入口から切り替えられるようにします。
public interface IIdempotencyStore
{
    Task<IdempotencyExecutionResult> ExecuteAsync(
        string clientScope,
        string key,
        string requestFingerprint,
        Func<Task<TodoItem>> operation,
        CancellationToken cancellationToken
    );
}
