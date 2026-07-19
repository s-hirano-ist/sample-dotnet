using System.Collections.Concurrent;

// TodoIdempotencyStoreは、同じクライアントが同じキーでPOSTを再送したとき、
// 最初に作成したTodoを返して重複作成を防ぎます。
// Singletonなので、現在は1つのAPIプロセス内でだけ有効です。
public sealed class TodoIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public async Task<IdempotencyExecutionResult> ExecuteAsync(
        string clientScope,
        string key,
        string requestFingerprint,
        Func<Task<TodoItem>> operation,
        CancellationToken cancellationToken
    )
    {
        var entryKey = $"{clientScope}:{key}";
        var newEntry = new Entry(
            requestFingerprint,
            new Lazy<Task<TodoItem>>(operation, LazyThreadSafetyMode.ExecutionAndPublication)
        );
        var entry = _entries.GetOrAdd(entryKey, newEntry);

        if (!string.Equals(entry.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            return IdempotencyExecutionResult.Conflict;
        }

        try
        {
            var todo = await entry.Operation.Value.WaitAsync(cancellationToken);
            var isReplay = !ReferenceEquals(entry, newEntry);
            return new IdempotencyExecutionResult(todo, isReplay);
        }
        catch
        {
            // 作成に失敗したキーは削除し、クライアントが同じキーで再試行できるようにします。
            _entries.TryRemove(new KeyValuePair<string, Entry>(entryKey, entry));
            throw;
        }
    }

    private sealed record Entry(
        string RequestFingerprint,
        Lazy<Task<TodoItem>> Operation
    );
}

public sealed record IdempotencyExecutionResult(TodoItem? Todo, bool IsReplay)
{
    public static IdempotencyExecutionResult Conflict => new(null, false);
}
