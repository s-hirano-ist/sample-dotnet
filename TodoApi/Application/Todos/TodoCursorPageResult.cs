// TodoCursorPageResultは、Repositoryが返すカーソル方式の検索結果です。
public sealed record TodoCursorPageResult(
    IReadOnlyList<TodoItem> Items,
    int PageSize,
    int? LastTodoId,
    bool HasNextPage
);
