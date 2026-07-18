using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

// RedisHealthCheckは、Redisへ接続して応答があるかを確認します。
// ヘルスチェックに含めることで、監視システムがRedis障害を検知できます。
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var latency = await _redis.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy(
                $"Redis responded in {latency.TotalMilliseconds:0} ms."
            );
        }
        catch (RedisException exception)
        {
            return HealthCheckResult.Unhealthy(
                "Redis is unavailable.",
                exception
            );
        }
    }
}
