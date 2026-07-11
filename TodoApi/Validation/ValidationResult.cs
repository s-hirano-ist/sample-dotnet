// バリデーション結果を表す型です。
// IsValid が true なら成功、false なら Error に理由が入ります。
public record ValidationResult(
    bool IsValid,
    ApiError? Error
)
{
    // static は、インスタンスを作らなくても ValidationResult.Success のように使えるメンバーです。
    public static ValidationResult Success => new(true, null);

    public static ValidationResult Failure(string code, string message)
    {
        return new ValidationResult(false, new ApiError(code, message));
    }
}
