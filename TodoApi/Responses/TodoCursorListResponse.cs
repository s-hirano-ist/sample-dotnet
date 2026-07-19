// TodoCursorListResponseは、カーソルページングの結果を表します。
public record TodoCursorListResponse(
    IReadOnlyList<TodoItem> Items,
    int PageSize,
    string? NextCursor,
    bool HasNextPage
);
