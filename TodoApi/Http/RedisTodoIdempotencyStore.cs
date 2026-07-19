using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

// RedisTodoIdempotencyStoreは、複数のAPIコンテナで冪等性キーを共有します。
public sealed class RedisTodoIdempotencyStore : IIdempotencyStore
{
    private const string KeyPrefix = "todo-api:idempotency:";
    private const string ReserveScript = """
        local current = redis.call('GET', KEYS[1])
        if current then
            return current
        end
        redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2])
        return ARGV[1]
        """;
    private const string CompleteScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            redis.call('SET', KEYS[1], ARGV[2], 'PX', ARGV[3])
            return 1
        end
        return 0
        """;
    private const string DeleteIfOwnerScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly IdempotencyOptions _options;

    public RedisTodoIdempotencyStore(
        IConnectionMultiplexer redis,
        IOptions<IdempotencyOptions> options
    )
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<IdempotencyExecutionResult> ExecuteAsync(
        string clientScope,
        string key,
        string requestFingerprint,
        Func<Task<TodoItem>> operation,
        CancellationToken cancellationToken
    )
    {
        var redisKey = new RedisKey(KeyPrefix + Hash($"{clientScope}:{key}"));
        var reservationId = Guid.NewGuid().ToString("N");
        var pendingRecord = new RedisIdempotencyRecord(requestFingerprint, reservationId, null);
        var pendingJson = JsonSerializer.Serialize(pendingRecord);
        var database = _redis.GetDatabase();

        var storedJson = (string?)await database.ScriptEvaluateAsync(
            ReserveScript,
            new[] { redisKey },
            new RedisValue[]
            {
                pendingJson,
                (long)TimeSpan.FromSeconds(_options.EntryLifetimeSeconds).TotalMilliseconds
            }
        );

        var storedRecord = JsonSerializer.Deserialize<RedisIdempotencyRecord>(storedJson ?? pendingJson)
            ?? throw new InvalidOperationException("Redis idempotency record could not be read.");

        if (!string.Equals(storedRecord.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            return IdempotencyExecutionResult.Conflict;
        }

        if (storedRecord.Todo is not null)
        {
            return new IdempotencyExecutionResult(storedRecord.Todo, true);
        }

        var isOwner = string.Equals(storedRecord.ReservationId, reservationId, StringComparison.Ordinal);

        if (!isOwner)
        {
            return await WaitForCompletionAsync(database, redisKey, requestFingerprint, cancellationToken);
        }

        try
        {
            var todo = await operation();
            var completedRecord = new RedisIdempotencyRecord(requestFingerprint, reservationId, todo);
            var completedJson = JsonSerializer.Serialize(completedRecord);
            await database.ScriptEvaluateAsync(
                CompleteScript,
                new[] { redisKey },
                new RedisValue[]
                {
                    pendingJson,
                    completedJson,
                    (long)TimeSpan.FromSeconds(_options.EntryLifetimeSeconds).TotalMilliseconds
                }
            );
            return new IdempotencyExecutionResult(todo, false);
        }
        catch
        {
            await database.ScriptEvaluateAsync(
                DeleteIfOwnerScript,
                new[] { redisKey },
                new RedisValue[] { pendingJson }
            );
            throw;
        }
    }

    private async Task<IdempotencyExecutionResult> WaitForCompletionAsync(
        IDatabase database,
        RedisKey redisKey,
        string requestFingerprint,
        CancellationToken cancellationToken
    )
    {
        var timeout = TimeSpan.FromSeconds(_options.WaitTimeoutSeconds);
        var startedAt = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            var json = (string?)await database.StringGetAsync(redisKey);

            if (json is null)
            {
                return IdempotencyExecutionResult.InProgress;
            }

            var record = JsonSerializer.Deserialize<RedisIdempotencyRecord>(json);

            if (record is null || !string.Equals(record.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
            {
                return IdempotencyExecutionResult.Conflict;
            }

            if (record.Todo is not null)
            {
                return new IdempotencyExecutionResult(record.Todo, true);
            }
        }

        return IdempotencyExecutionResult.InProgress;
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private sealed record RedisIdempotencyRecord(
        string RequestFingerprint,
        string ReservationId,
        TodoItem? Todo
    );
}
