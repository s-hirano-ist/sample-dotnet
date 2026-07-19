using Microsoft.Extensions.Logging;

// ApiLogEventsは、運用で検索・集計するログイベントをまとめます。
public static class ApiLogEvents
{
    public static readonly EventId InvalidApiKey = new(1001, nameof(InvalidApiKey));
    public static readonly EventId HttpRequestCompleted = new(1002, nameof(HttpRequestCompleted));
    public static readonly EventId UnhandledException = new(1003, nameof(UnhandledException));
    public static readonly EventId RequestCanceled = new(1004, nameof(RequestCanceled));
}
