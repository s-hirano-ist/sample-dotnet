// TodoValidationは、Todoの入力チェックをまとめるクラスです。
// static class は、newせずに TodoValidation.ValidateTitle(...) のように使います。
public static class TodoValidation
{
    // const はコンパイル時に決まる定数です。
    // API層からも同じドメインルールを使い、Entityと制約値がずれないようにします。
    public const int MaxTitleLength = TodoRules.MaxTitleLength;

    public static ValidationResult ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ValidationResult.Failure(
                code: "title_required",
                message: "Title is required."
            );
        }

        if (title.Length > MaxTitleLength)
        {
            return ValidationResult.Failure(
                code: "title_too_long",
                message: $"Title must be {MaxTitleLength} characters or fewer."
            );
        }

        return ValidationResult.Success;
    }

    public static ValidationResult ValidateOptionalTitle(string? title)
    {
        // 更新時はタイトルを送らないことを許可します。
        // nullの場合は「タイトルは変更しない」という意味にしています。
        if (title is null)
        {
            return ValidationResult.Success;
        }

        return ValidateTitle(title);
    }
}
