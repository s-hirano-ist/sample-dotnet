using Microsoft.Extensions.Logging;

// ApiLogEventsは、運用で検索・集計するログイベントをまとめます。
public static class ApiLogEvents
{
    public const int InvalidApiKeyId = 1001;
    public const int ApiKeyAuthenticatedId = 1005;
    public const int HttpRequestCompletedId = 1002;
    public const int UnhandledExceptionId = 1003;
    public const int RequestCanceledId = 1004;
    public const int TodoCreatedId = 1101;
    public const int TodoUpdatedId = 1102;
    public const int TodoDeletedId = 1103;
    public const int TodoUpdateNotFoundId = 1104;
    public const int TodoDeleteNotFoundId = 1105;
    public const int RedisRateLimitFailedId = 1201;

    public static readonly EventId InvalidApiKey = new(InvalidApiKeyId, nameof(InvalidApiKey));
    public static readonly EventId HttpRequestCompleted = new(HttpRequestCompletedId, nameof(HttpRequestCompleted));
}
