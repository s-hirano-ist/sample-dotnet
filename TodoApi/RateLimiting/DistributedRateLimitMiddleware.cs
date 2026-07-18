using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

// DistributedRateLimitMiddlewareは、Redisを共有カウンターとして使うレート制限です。
// 複数のECSタスクやコンテナから同じRedisを見ることで、制限状態を共有できます。
public class DistributedRateLimitMiddleware
{
    private const string IncrementScript = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        return current
        """;

    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DistributedRateLimitMiddleware> _logger;

    public DistributedRateLimitMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<DistributedRateLimitMiddleware> logger
    )
    {
        _next = next;
        _redis = redis;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var permitLimit = _configuration.GetValue<int>("RateLimit:PermitLimit", 10);
        var windowSeconds = _configuration.GetValue<int>("RateLimit:WindowSeconds", 10);
        var partitionKey = GetPartitionKey(context);
        var redisKey = $"todo-api:rate-limit:{Hash(partitionKey)}";

        try
        {
            var database = _redis.GetDatabase();
            var currentCount = (long)await database.ScriptEvaluateAsync(
                IncrementScript,
                new RedisKey[] { redisKey },
                new RedisValue[] { windowSeconds }
            );

            if (currentCount > permitLimit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = windowSeconds.ToString();
                return;
            }

            await _next(context);
        }
        catch (RedisException exception)
        {
            // レート制限を共有できない場合は、制限を無視して処理を続けません。
            // 共有ストア障害時に保護を外さないため、503を返します。
            _logger.LogError(exception, "Redis rate limit check failed");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
    }

    private static string GetPartitionKey(HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated == true
            ? $"user:{context.User.Identity.Name}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
