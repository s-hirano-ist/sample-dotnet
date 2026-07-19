using Microsoft.Extensions.Options;

// IdempotencyOptionsValidatorは、短すぎる・長すぎる保持期間を起動時に検出します。
public sealed class IdempotencyOptionsValidator : IValidateOptions<IdempotencyOptions>
{
    public ValidateOptionsResult Validate(string? name, IdempotencyOptions options)
    {
        return options.EntryLifetimeSeconds is < 1 or > 86_400
            ? ValidateOptionsResult.Fail(
                "Idempotency:EntryLifetimeSeconds must be between 1 and 86400."
            )
            : ValidateOptionsResult.Success;
    }
}
