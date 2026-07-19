using System.Diagnostics;
using System.Diagnostics.Metrics;

// ApiMetricsは、APIの状態を数値として記録するメトリクスです。
// ログと違い、ダッシュボードやアラートで集計する用途に向いています。
public sealed class ApiMetrics
{
    public const string MeterName = "SampleDotnet.TodoApi";
    public const string RequestCounterName = "todo_api.http.requests";
    public const string RequestDurationName = "todo_api.http.request.duration";

    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;

    public ApiMetrics()
    {
        var meter = new Meter(MeterName, "1.0");
        _requestCounter = meter.CreateCounter<long>(
            RequestCounterName,
            unit: "{request}",
            description: "Number of completed HTTP requests."
        );
        _requestDuration = meter.CreateHistogram<double>(
            RequestDurationName,
            unit: "ms",
            description: "HTTP request duration in milliseconds."
        );
    }

    public void RecordRequest(string method, int statusCode, double durationMilliseconds)
    {
        var tags = new TagList
        {
            { "http.method", method },
            { "http.status_code", statusCode }
        };

        _requestCounter.Add(1, tags);
        _requestDuration.Record(durationMilliseconds, tags);
    }
}
