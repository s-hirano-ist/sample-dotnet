// TodoItemは、APIの中で扱うTodoのデータ構造です。
// EF Coreでは、このようなデータベースに保存する型をEntityと呼びます。
public class TodoItem
{
    // Idは主キーです。SQLiteでは整数の主キーが自動採番されます。
    public int Id { get; private set; }

    public string Title { get; private set; }

    public bool IsDone { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

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

    // Createは、外部入力から新しいTodoを作るためのFactoryです。
    // Entityを直接newする代わりに、作成時のルールをここへ集めます。
    public static DomainResult<TodoItem> Create(string title, DateTimeOffset createdAt)
    {
        var validation = ValidateTitle(title);
        if (!validation.IsSuccess)
        {
            return DomainResult<TodoItem>.Failure(
                validation.Error!.Code,
                validation.Error.Message
            );
        }

        return DomainResult<TodoItem>.Success(
            new TodoItem(
                Id: 0,
                Title: title,
                IsDone: false,
                CreatedAt: createdAt,
                CompletedAt: null
            )
        );
    }

    // ChangeTitleは、タイトルを変更するドメイン操作です。
    public DomainResult ChangeTitle(string title)
    {
        var validation = ValidateTitle(title);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        Title = title;
        return DomainResult.Success;
    }

    // Completeは、Todoを完了状態へ変更します。
    // すでに完了している場合は、最初に完了した日時を維持します。
    public DomainResult Complete(DateTimeOffset completedAt)
    {
        IsDone = true;
        CompletedAt ??= completedAt;
        return DomainResult.Success;
    }

    // Reopenは、Todoを未完了状態へ戻し、完了日時を消します。
    public DomainResult Reopen()
    {
        IsDone = false;
        CompletedAt = null;
        return DomainResult.Success;
    }

    private static DomainResult ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return DomainResult.Failure("title_required", "Title is required.");
        }

        if (title.Length > TodoRules.MaxTitleLength)
        {
            return DomainResult.Failure(
                "title_too_long",
                $"Title must be {TodoRules.MaxTitleLength} characters or fewer."
            );
        }

        return DomainResult.Success;
    }
}
