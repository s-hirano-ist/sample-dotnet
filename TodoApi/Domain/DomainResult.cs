// DomainResultは、ドメイン処理の成功または失敗を表します。
// 例外ではなく戻り値でルール違反を表すため、通常の失敗を明示的に扱えます。
public sealed record DomainResult(bool IsSuccess, DomainError? Error)
{
    public static DomainResult Success => new(true, null);

    public static DomainResult Failure(string code, string message)
    {
        return new DomainResult(false, new DomainError(code, message));
    }
}

// DomainResult<T>は、ドメイン処理が値も返す場合に使います。
public sealed record DomainResult<T>(bool IsSuccess, T? Value, DomainError? Error)
{
    public static DomainResult<T> Success(T value) => new(true, value, null);

    public static DomainResult<T> Failure(string code, string message)
    {
        return new DomainResult<T>(false, default, new DomainError(code, message));
    }
}
