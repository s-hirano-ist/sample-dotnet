using Microsoft.Extensions.Options;

// CorsOptionsValidatorは、CORS設定の妥当性を検証します。
public sealed class CorsOptionsValidator : IValidateOptions<CorsOptions>
{
    public ValidateOptionsResult Validate(string? name, CorsOptions options)
    {
        var originsAreValid =
            options.AllowedOrigins.Length > 0
            && options.AllowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            );

        if (!originsAreValid)
        {
            return ValidateOptionsResult.Fail(
                "Cors:AllowedOrigins must contain at least one absolute HTTP or HTTPS origin."
            );
        }

        if (options.AllowedMethods.Length == 0 || options.AllowedHeaders.Length == 0)
        {
            return ValidateOptionsResult.Fail(
                "Cors:AllowedMethods and Cors:AllowedHeaders must not be empty."
            );
        }

        return ValidateOptionsResult.Success;
    }
}
