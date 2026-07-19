using Microsoft.Extensions.Options;

// RateLimitOptionsValidatorは、レート制限設定の妥当性を検証します。
public sealed class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        var storeIsValid = string.Equals(options.Store, "Memory", StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.Store, "Redis", StringComparison.OrdinalIgnoreCase);

        if (!storeIsValid || options.PermitLimit <= 0 || options.WindowSeconds <= 0)
        {
            return ValidateOptionsResult.Fail(
                "RateLimit:Store must be Memory or Redis, and limits must be greater than zero."
            );
        }

        return ValidateOptionsResult.Success;
    }
}
