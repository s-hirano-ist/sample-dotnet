// TodoListQueryは、一覧取得に必要な条件をApplication層で表します。
public sealed record TodoListQuery(
    int Page,
    int PageSize,
    bool? IsDone,
    string? Search,
    string? SortBy,
    string? SortOrder
);
