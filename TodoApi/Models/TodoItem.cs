// TodoItemは、APIの中で扱うTodoのデータ構造です。
// EF Coreでは、このようなデータベースに保存する型をEntityと呼びます。
public class TodoItem
{
    private readonly List<IDomainEvent> _domainEvents = [];

    // Idは主キーです。SQLiteでは整数の主キーが自動採番されます。
    public int Id { get; private set; }

    public string Title { get; private set; }

    public bool IsDone { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    // 発生したイベントを読み取り専用で公開します。
    // Domain内部の状態なので、APIのJSONレスポンスには含めません。
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

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
        var todoTitle = TodoTitle.Create(title);
        if (!todoTitle.IsSuccess)
        {
            return DomainResult<TodoItem>.Failure(
                todoTitle.Error!.Code,
                todoTitle.Error.Message
            );
        }

        return DomainResult<TodoItem>.Success(
            new TodoItem(
                Id: 0,
                Title: todoTitle.Value!.Value,
                IsDone: false,
                CreatedAt: createdAt,
                CompletedAt: null
            )
        );
    }

    // ChangeTitleは、タイトルを変更するドメイン操作です。
    public DomainResult ChangeTitle(string title)
    {
        var todoTitle = TodoTitle.Create(title);
        if (!todoTitle.IsSuccess)
        {
            return DomainResult.Failure(
                todoTitle.Error!.Code,
                todoTitle.Error.Message
            );
        }

        if (!string.Equals(Title, todoTitle.Value!.Value, StringComparison.Ordinal))
        {
            Title = todoTitle.Value.Value;
            AddDomainEvent(new TodoTitleChangedDomainEvent(Id));
        }
        return DomainResult.Success;
    }

    // Completeは、Todoを完了状態へ変更します。
    // すでに完了している場合は、最初に完了した日時を維持します。
    public DomainResult Complete(DateTimeOffset completedAt)
    {
        var wasDone = IsDone;
        IsDone = true;
        CompletedAt ??= completedAt;
        if (!wasDone)
        {
            AddDomainEvent(new TodoCompletedDomainEvent(Id, CompletedAt.Value));
        }
        return DomainResult.Success;
    }

    // Reopenは、Todoを未完了状態へ戻し、完了日時を消します。
    public DomainResult Reopen()
    {
        var wasDone = IsDone;
        IsDone = false;
        CompletedAt = null;
        if (wasDone)
        {
            AddDomainEvent(new TodoReopenedDomainEvent(Id));
        }
        return DomainResult.Success;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    // Dispatcherへ渡したイベントをEntityから取り出し、二重処理を防ぎます。
    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var domainEvents = _domainEvents.ToArray();
        _domainEvents.Clear();
        return domainEvents;
    }

}
