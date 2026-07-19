using Microsoft.Extensions.Options;

// ApiKeyOptionsValidatorは、APIキー設定の妥当性を検証します。
public sealed class ApiKeyOptionsValidator : IValidateOptions<ApiKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiKeyOptions options)
    {
        var hasLegacyConfiguration =
            !string.IsNullOrWhiteSpace(options.ApiKey) && options.Permissions.Length > 0;
        var hasClientConfiguration = options.Clients.Any(client =>
            !string.IsNullOrWhiteSpace(client.Key) && client.Permissions.Length > 0
        );

        if (!hasLegacyConfiguration && !hasClientConfiguration)
        {
            return ValidateOptionsResult.Fail(
                "Authentication must configure a legacy API key or at least one client."
            );
        }

        if (options.Clients.Any(client =>
                string.IsNullOrWhiteSpace(client.Name)
                || string.IsNullOrWhiteSpace(client.Key)
                || client.Permissions.Length == 0
            ))
        {
            return ValidateOptionsResult.Fail(
                "Each Authentication client must have a name, key, and permission."
            );
        }

        if (options.AdditionalApiKeys.Any(string.IsNullOrWhiteSpace))
        {
            return ValidateOptionsResult.Fail("Additional API keys must not be empty.");
        }

        var keys = new[] { options.ApiKey }
            .Concat(options.AdditionalApiKeys)
            .Concat(options.Clients.Select(client => client.Key))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToArray();

        if (keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
        {
            return ValidateOptionsResult.Fail("Authentication API keys must be unique.");
        }

        return ValidateOptionsResult.Success;
    }
}
