using Microsoft.Extensions.Options;

// IdempotencyOptionsValidatorは、短すぎる・長すぎる保持期間を起動時に検出します。
public sealed class IdempotencyOptionsValidator : IValidateOptions<IdempotencyOptions>
{
    public ValidateOptionsResult Validate(string? name, IdempotencyOptions options)
    {
        if (!string.Equals(options.Store, "Memory", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(options.Store, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("Idempotency:Store must be Memory or Redis.");
        }

        if (options.EntryLifetimeSeconds is < 1 or > 86_400)
        {
            return ValidateOptionsResult.Fail(
                "Idempotency:EntryLifetimeSeconds must be between 1 and 86400."
            );
        }

        return options.WaitTimeoutSeconds is < 1 or > 300
            ? ValidateOptionsResult.Fail("Idempotency:WaitTimeoutSeconds must be between 1 and 300.")
            : ValidateOptionsResult.Success;
    }
}
