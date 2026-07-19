using Microsoft.Extensions.Logging;

// ApiLogEventsは、運用で検索・集計するログイベントをまとめます。
public static class ApiLogEvents
{
    public static readonly EventId InvalidApiKey = new(1001, nameof(InvalidApiKey));
    public static readonly EventId HttpRequestCompleted = new(1002, nameof(HttpRequestCompleted));
    public static readonly EventId UnhandledException = new(1003, nameof(UnhandledException));
    public static readonly EventId RequestCanceled = new(1004, nameof(RequestCanceled));
    public static readonly EventId TodoCreated = new(1101, nameof(TodoCreated));
    public static readonly EventId TodoUpdated = new(1102, nameof(TodoUpdated));
    public static readonly EventId TodoDeleted = new(1103, nameof(TodoDeleted));
    public static readonly EventId TodoUpdateNotFound = new(1104, nameof(TodoUpdateNotFound));
    public static readonly EventId TodoDeleteNotFound = new(1105, nameof(TodoDeleteNotFound));
}
