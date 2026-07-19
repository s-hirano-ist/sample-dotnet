using Microsoft.Extensions.Options;

namespace TodoApi.Tests;

public class OpenTelemetryOptionsValidatorTests
{
    [Fact]
    public void Validate_WhenDisabled_AllowsMissingEndpoint()
    {
        var validator = new OpenTelemetryOptionsValidator();

        var result = validator.Validate(
            Options.DefaultName,
            new OpenTelemetryOptions { Enabled = false }
        );

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_WhenEnabled_RejectsInvalidEndpoint()
    {
        var validator = new OpenTelemetryOptionsValidator();

        var result = validator.Validate(
            Options.DefaultName,
            new OpenTelemetryOptions { Enabled = true, OtlpEndpoint = "not-a-url" }
        );

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_WhenEnabled_AcceptsHttpEndpoint()
    {
        var validator = new OpenTelemetryOptionsValidator();

        var result = validator.Validate(
            Options.DefaultName,
            new OpenTelemetryOptions
            {
                Enabled = true,
                OtlpEndpoint = "http://otel-collector:4317"
            }
        );

        Assert.Equal(ValidateOptionsResult.Success, result);
    }
}
