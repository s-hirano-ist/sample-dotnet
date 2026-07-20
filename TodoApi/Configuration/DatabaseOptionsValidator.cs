using Microsoft.Extensions.Options;

// DatabaseOptionsValidatorは、未対応のDBプロバイダーを起動時に検出します。
public sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        return string.Equals(options.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.Provider, "Postgres", StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.Provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Database:Provider must be Sqlite or Postgres.");
    }
}
