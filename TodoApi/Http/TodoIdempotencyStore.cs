using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

// TodoIdempotencyStoreは、同じクライアントが同じキーでPOSTを再送したとき、
// 最初に作成したTodoを返して重複作成を防ぎます。
// Singletonなので、現在は1つのAPIプロセス内でだけ有効です。
public sealed class TodoIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private readonly IdempotencyOptions _options;
    private readonly TimeProvider _timeProvider;

    public TodoIdempotencyStore(IOptions<IdempotencyOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<IdempotencyExecutionResult> ExecuteAsync(
        string clientScope,
        string key,
        string requestFingerprint,
        Func<Task<TodoItem>> operation,
        CancellationToken cancellationToken
    )
    {
        var entryKey = $"{clientScope}:{key}";
        var now = _timeProvider.GetUtcNow();
        RemoveExpiredEntries(now);

        Entry entry;
        var isNewEntry = false;

        while (true)
        {
            if (_entries.TryGetValue(entryKey, out var existingEntry))
            {
                if (existingEntry.ExpiresAt > now)
                {
                    entry = existingEntry;
                    break;
                }

                _entries.TryRemove(new KeyValuePair<string, Entry>(entryKey, existingEntry));
                continue;
            }

            var newEntry = new Entry(
                requestFingerprint,
                new Lazy<Task<TodoItem>>(operation, LazyThreadSafetyMode.ExecutionAndPublication),
                now.Add(TimeSpan.FromSeconds(_options.EntryLifetimeSeconds))
            );

            if (_entries.TryAdd(entryKey, newEntry))
            {
                entry = newEntry;
                isNewEntry = true;
                break;
            }
        }

        if (!string.Equals(entry.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            return IdempotencyExecutionResult.Conflict;
        }

        try
        {
            var todo = await entry.Operation.Value.WaitAsync(cancellationToken);
            var isReplay = !isNewEntry;
            return new IdempotencyExecutionResult(todo, isReplay);
        }
        catch
        {
            // 作成に失敗したキーは削除し、クライアントが同じキーで再試行できるようにします。
            _entries.TryRemove(new KeyValuePair<string, Entry>(entryKey, entry));
            throw;
        }
    }

    private void RemoveExpiredEntries(DateTimeOffset now)
    {
        foreach (var pair in _entries)
        {
            if (pair.Value.ExpiresAt <= now)
            {
                _entries.TryRemove(new KeyValuePair<string, Entry>(pair.Key, pair.Value));
            }
        }
    }

    private sealed record Entry(
        string RequestFingerprint,
        Lazy<Task<TodoItem>> Operation,
        DateTimeOffset ExpiresAt
    );
}

public sealed record IdempotencyExecutionResult(TodoItem? Todo, bool IsReplay)
{
    public static IdempotencyExecutionResult Conflict => new(null, false);
}
