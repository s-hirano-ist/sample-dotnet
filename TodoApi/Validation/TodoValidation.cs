// TodoValidationは、Todoの入力チェックをまとめるクラスです。
// static class は、newせずに TodoValidation.ValidateTitle(...) のように使います。
public static class TodoValidation
{
    // const はコンパイル時に決まる定数です。
    // API層からも同じドメインルールを使い、Entityと制約値がずれないようにします。
    public const int MaxTitleLength = TodoRules.MaxTitleLength;

    public static ValidationResult ValidateTitle(string? title)
    {
        var result = TodoTitle.Create(title);
        if (!result.IsSuccess)
        {
            return ValidationResult.Failure(
                code: result.Error!.Code,
                message: result.Error.Message
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
