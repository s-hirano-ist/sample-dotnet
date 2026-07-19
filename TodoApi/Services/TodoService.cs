using Microsoft.EntityFrameworkCore;

// TodoServiceは、Todoを操作する処理をまとめたクラスです。
// 今回からListではなく、Entity Framework Coreを通してSQLiteに保存します。
public partial class TodoService
{
    private readonly TodoDbContext _dbContext;
    private readonly ILogger<TodoService> _logger;

    // コンストラクタでTodoDbContextとILoggerを受け取ります。
    // ASP.NET CoreのDIが、必要なオブジェクトを自動で渡してくれます。
    public TodoService(TodoDbContext dbContext, ILogger<TodoService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
        // IQueryableは、まだDBへ実行していない検索処理を表します。
        // 条件を追加してからCountAsyncやToListAsyncを呼ぶと、SQLとして実行されます。
        var query = _dbContext.Todos.AsNoTracking();

        if (isDone.HasValue)
        {
            query = query.Where(todo => todo.IsDone == isDone.Value);
        }

        // Trimで前後の空白を取り除き、空文字列は検索条件にしません。
        var searchTerm = search?.Trim() ?? string.Empty;

        if (searchTerm.Length > 0)
        {
            // DBによって文字列比較の大文字・小文字の扱いが異なるため、
            // 両方を小文字化して比較し、APIの挙動を一定にします。
            var normalizedSearchTerm = searchTerm.ToLowerInvariant();
            query = query.Where(todo => todo.Title.ToLower().Contains(normalizedSearchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var normalizedSortBy = (sortBy ?? TodoSortValidation.DefaultSortBy).Trim().ToLowerInvariant();
        var normalizedSortOrder = (sortOrder ?? TodoSortValidation.DefaultSortOrder).Trim().ToLowerInvariant();

        // 外部入力をそのままSQLに渡さず、許可したプロパティだけを分岐で選びます。
        query = normalizedSortBy switch
        {
            "title" when normalizedSortOrder == "desc" => query.OrderByDescending(todo => todo.Title).ThenByDescending(todo => todo.Id),
            "title" => query.OrderBy(todo => todo.Title).ThenBy(todo => todo.Id),
            "createdat" when normalizedSortOrder == "desc" => query.OrderByDescending(todo => todo.CreatedAt).ThenByDescending(todo => todo.Id),
            "createdat" => query.OrderBy(todo => todo.CreatedAt).ThenBy(todo => todo.Id),
            "id" when normalizedSortOrder == "desc" => query.OrderByDescending(todo => todo.Id),
            _ => query.OrderBy(todo => todo.Id)
        };

        var todos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // 整数の割り算で余りがある場合にも、最後のページを1ページとして数えます。
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new TodoListResponse(
            Items: todos,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        // FirstOrDefaultAsync は、条件に合う最初の要素をデータベースから探します。
        // 見つからない場合は null を返します。
        return await _dbContext.Todos.FirstOrDefaultAsync(todo => todo.Id == id, cancellationToken);
    }

    public async Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken)
    {
        // IdはSQLiteが自動採番するので、ここでは指定しません。
        var todo = new TodoItem(
            Id: 0,
            Title: request.Title,
            IsDone: false,
            CreatedAt: DateTimeOffset.UtcNow,
            CompletedAt: null
        );

        _dbContext.Todos.Add(todo);

        // SaveChangesAsync を呼ぶと、変更内容がSQLiteに保存されます。
        // ここでSQLiteがIdを採番し、todo.Idにも反映されます。
        await _dbContext.SaveChangesAsync(cancellationToken);

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

        // ?? は null合体演算子です。
        // 左側がnullでなければ左側、nullなら右側を使います。
        var isDone = request.IsDone ?? existingTodo.IsDone;

        existingTodo.Title = request.Title ?? existingTodo.Title;
        existingTodo.IsDone = isDone;
        // 完了状態なら完了日時を入れ、未完了なら null に戻します。
        existingTodo.CompletedAt = isDone
            ? existingTodo.CompletedAt ?? DateTimeOffset.UtcNow
            : null;

        await _dbContext.SaveChangesAsync(cancellationToken);

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

        _dbContext.Todos.Remove(todo);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
}
