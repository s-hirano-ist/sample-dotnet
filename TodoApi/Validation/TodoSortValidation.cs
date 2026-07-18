// Todo一覧で指定できる並び替え項目を検証します。
public static class TodoSortValidation
{
    public const string DefaultSortBy = "id";
    public const string DefaultSortOrder = "asc";

    public static ValidationResult Validate(string? sortBy, string? sortOrder)
    {
        var normalizedSortBy = (sortBy ?? DefaultSortBy).Trim().ToLowerInvariant();
        var normalizedSortOrder = (sortOrder ?? DefaultSortOrder).Trim().ToLowerInvariant();

        if (normalizedSortBy is not ("id" or "title" or "createdat"))
        {
            return ValidationResult.Failure(
                "sort_by_invalid",
                "SortBy must be id, title, or createdAt."
            );
        }

        if (normalizedSortOrder is not ("asc" or "desc"))
        {
            return ValidationResult.Failure(
                "sort_order_invalid",
                "SortOrder must be asc or desc."
            );
        }

        return ValidationResult.Success;
    }
}
