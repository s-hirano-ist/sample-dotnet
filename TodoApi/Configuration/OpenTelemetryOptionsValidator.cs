using Microsoft.Extensions.Options;

// OpenTelemetryOptionsValidatorは、OpenTelemetryを有効にしたときだけ送信先を検証します。
public sealed class OpenTelemetryOptionsValidator : IValidateOptions<OpenTelemetryOptions>
{
    public ValidateOptionsResult Validate(string? name, OpenTelemetryOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (
            !Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
        )
        {
            return ValidateOptionsResult.Fail(
                "OpenTelemetry:OtlpEndpoint must be an absolute HTTP or HTTPS URL when enabled."
            );
        }

        return ValidateOptionsResult.Success;
    }
}
