using System.Diagnostics.Metrics;

namespace TodoApi.Tests;

public class ApiMetricsTests
{
    [Fact]
    public void RecordRequest_EmitsCounterAndDurationMeasurements()
    {
        long requestCount = 0;
        double duration = 0;

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == ApiMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == ApiMetrics.RequestCounterName)
            {
                requestCount += measurement;
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == ApiMetrics.RequestDurationName)
            {
                duration = measurement;
            }
        });
        listener.Start();

        var metrics = new ApiMetrics();
        metrics.RecordRequest("GET", 200, 12.5);

        Assert.Equal(1, requestCount);
        Assert.Equal(12.5, duration);
    }
}
