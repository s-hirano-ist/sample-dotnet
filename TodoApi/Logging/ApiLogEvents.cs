using Microsoft.Extensions.Logging;

// ApiLogEventsは、運用で検索・集計するログイベントをまとめます。
public static class ApiLogEvents
{
    public static readonly EventId InvalidApiKey = new(1001, nameof(InvalidApiKey));
}
