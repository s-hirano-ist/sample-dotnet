// TodoIdempotencyKeyは、Idempotency-Keyヘッダーの入力を検証します。
public static class TodoIdempotencyKey
{
    public static ValidationResult Validate(HttpRequest request, out string? key)
    {
        key = null;

        if (!request.Headers.TryGetValue(TodoIdempotencyDefaults.HeaderName, out var values))
        {
            return ValidationResult.Success;
        }

        var value = values.ToString().Trim();

        if (value.Length == 0 || value.Length > TodoIdempotencyDefaults.MaxKeyLength)
        {
            return ValidationResult.Failure(
                "idempotency_key_invalid",
                $"Idempotency-Key must be between 1 and {TodoIdempotencyDefaults.MaxKeyLength} characters."
            );
        }

        key = value;
        return ValidationResult.Success;
    }
}
