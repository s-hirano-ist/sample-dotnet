// TodoListResponseは、Todo一覧とページング情報をまとめたレスポンスです。
public record TodoListResponse(
    IReadOnlyList<TodoItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
