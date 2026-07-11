// Todo更新時に受け取るJSONの形です。
// string? や bool? の ? は、nullを許可するという意味です。
// 例: { "isDone": true }
public record UpdateTodoRequest(
    string? Title,
    bool? IsDone
);
