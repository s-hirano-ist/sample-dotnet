// TodoCursorQueryは、カーソルページングに必要な条件を表します。
public sealed record TodoCursorQuery(
    int PageSize,
    int? AfterId,
    bool? IsDone,
    string? Search
);
