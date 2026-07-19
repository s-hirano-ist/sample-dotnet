// TodoPageResultは、Repositoryが返すページ番号方式の検索結果です。
public sealed record TodoPageResult(
    IReadOnlyList<TodoItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
