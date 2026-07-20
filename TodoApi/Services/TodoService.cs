// TodoServiceは、Todoを操作する処理をまとめたクラスです。
// DBアクセスの詳細はITodoRepositoryへ隠し、業務ルールを担当します。
public partial class TodoService
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<TodoService> _logger;
    private readonly TimeProvider _timeProvider;

    // コンストラクタでRepositoryとILoggerを受け取ります。
    // ASP.NET CoreのDIが、必要なオブジェクトを自動で渡してくれます。
    public TodoService(
        ITodoRepository repository,
        ILogger<TodoService> logger,
        TimeProvider timeProvider
    )
    {
        _repository = repository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    // SkipとTakeを使い、必要なページのTodoだけをデータベースから読み取ります。
    // ページングすると、Todoが大量にあっても全件をメモリへ読み込まずに済みます。
    public async Task<TodoListResponse> GetPageAsync(
        int page,
        int pageSize,
        bool? isDone,
        string? search,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken
    )
    {
        var result = await _repository.GetPageAsync(
            new TodoListQuery(page, pageSize, isDone, search, sortBy, sortOrder),
            cancellationToken
        );

        return new TodoListResponse(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages
        );
    }

    public async Task<TodoCursorListResponse> GetCursorPageAsync(
        int pageSize,
        int? afterId,
        bool? isDone,
        string? search,
        CancellationToken cancellationToken
    )
    {
        var result = await _repository.GetCursorPageAsync(
            new TodoCursorQuery(pageSize, afterId, isDone, search),
            cancellationToken
        );

        var nextCursor = result.HasNextPage && result.LastTodoId.HasValue
            ? TodoCursor.Create(result.LastTodoId.Value)
            : null;

        return new TodoCursorListResponse(
            Items: result.Items,
            PageSize: pageSize,
            NextCursor: nextCursor,
            HasNextPage: result.HasNextPage
        );
    }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        // FirstOrDefaultAsync は、条件に合う最初の要素をデータベースから探します。
        // 見つからない場合は null を返します。
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var creation = TodoItem.Create(request.Title, _timeProvider.GetUtcNow());
        var todo = RequireValue(creation);

        _repository.Add(todo);

        // SaveChangesAsyncを呼ぶと、Repositoryの実装先へ保存されます。
        // ここでデータベースがIdを採番し、todo.Idにも反映されます。
        await _repository.SaveChangesAsync(cancellationToken);

        // {TodoId}は構造化ログのプレースホルダーです。
        // タイトル本文はログに出さず、操作とIDだけを記録します。
        LogTodoCreated(todo.Id);

        return todo;
    }

    public async Task<TodoItem?> UpdateAsync(
        int id,
        UpdateTodoRequest request,
        CancellationToken cancellationToken
    )
    {
        var existingTodo = await GetByIdAsync(id, cancellationToken);

        if (existingTodo is null)
        {
            LogTodoUpdateNotFound(id);
            return null;
        }

        if (request.Title is not null)
        {
            RequireSuccess(existingTodo.ChangeTitle(request.Title));
        }

        if (request.IsDone is true)
        {
            RequireSuccess(existingTodo.Complete(_timeProvider.GetUtcNow()));
        }
        else if (request.IsDone is false)
        {
            RequireSuccess(existingTodo.Reopen());
        }

        await _repository.SaveChangesAsync(cancellationToken);

        LogTodoUpdated(id);

        return existingTodo;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var todo = await GetByIdAsync(id, cancellationToken);

        if (todo is null)
        {
            LogTodoDeleteNotFound(id);
            return false;
        }

        _repository.Remove(todo);
        await _repository.SaveChangesAsync(cancellationToken);

        LogTodoDeleted(id);

        return true;
    }

    [LoggerMessage(EventId = ApiLogEvents.TodoCreatedId, Level = LogLevel.Information, Message = "Created todo with id {TodoId}")]
    private partial void LogTodoCreated(int todoId);

    [LoggerMessage(EventId = ApiLogEvents.TodoUpdatedId, Level = LogLevel.Information, Message = "Updated todo with id {TodoId}")]
    private partial void LogTodoUpdated(int todoId);

    [LoggerMessage(EventId = ApiLogEvents.TodoDeletedId, Level = LogLevel.Information, Message = "Deleted todo with id {TodoId}")]
    private partial void LogTodoDeleted(int todoId);

    [LoggerMessage(EventId = ApiLogEvents.TodoUpdateNotFoundId, Level = LogLevel.Warning, Message = "Todo with id {TodoId} was not found for update")]
    private partial void LogTodoUpdateNotFound(int todoId);

    [LoggerMessage(EventId = ApiLogEvents.TodoDeleteNotFoundId, Level = LogLevel.Warning, Message = "Todo with id {TodoId} was not found for delete")]
    private partial void LogTodoDeleteNotFound(int todoId);

    private static TodoItem RequireValue(DomainResult<TodoItem> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return result.Value;
        }

        throw new InvalidOperationException(
            $"Todo domain rule failed: {result.Error?.Code ?? "unknown"}"
        );
    }

    private static void RequireSuccess(DomainResult result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Todo domain rule failed: {result.Error?.Code ?? "unknown"}"
            );
        }
    }
}
