// TodoItemは、APIの中で扱うTodoのデータ構造です。
// EF Coreでは、このようなデータベースに保存する型をEntityと呼びます。
public class TodoItem
{
    // Idは主キーです。SQLiteでは整数の主キーが自動採番されます。
    public int Id { get; set; }

    public string Title { get; set; }

    public bool IsDone { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    // EF Coreがデータベースから値を読み込むときに使うコンストラクタです。
    private TodoItem()
    {
        Title = string.Empty;
    }

    public TodoItem(
        int Id,
        string Title,
        bool IsDone,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt
    )
    {
        this.Id = Id;
        this.Title = Title;
        this.IsDone = IsDone;
        this.CreatedAt = CreatedAt;
        this.CompletedAt = CompletedAt;
    }
}
