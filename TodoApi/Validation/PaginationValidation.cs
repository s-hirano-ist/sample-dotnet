// 一覧取得のページ番号と1ページあたりの件数を検証します。
public static class PaginationValidation
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static ValidationResult Validate(int page, int pageSize)
    {
        if (page < 1)
        {
            return ValidationResult.Failure("page_invalid", "Page must be 1 or greater.");
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            return ValidationResult.Failure(
                "page_size_invalid",
                $"PageSize must be between 1 and {MaxPageSize}."
            );
        }

        return ValidationResult.Success;
    }
}
