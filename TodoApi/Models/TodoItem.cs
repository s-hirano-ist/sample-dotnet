// TodoItemは、APIの中で扱うTodoのデータ構造です。
// recordは「値を表す型」を短く書けるC#の構文です。
public record TodoItem(
    int Id,
    string Title,
    bool IsDone,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);
