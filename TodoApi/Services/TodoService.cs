using Microsoft.EntityFrameworkCore;

// TodoServiceは、Todoを操作する処理をまとめたクラスです。
// 今回からListではなく、Entity Framework Coreを通してSQLiteに保存します。
public class TodoService
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
    public async Task<TodoListResponse> GetPageAsync(int page, int pageSize)
    {
        var totalCount = await _dbContext.Todos.CountAsync();

        var todos = await _dbContext.Todos
            .AsNoTracking()
            .OrderBy(todo => todo.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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

    public async Task<TodoItem?> GetByIdAsync(int id)
    {
        // FirstOrDefaultAsync は、条件に合う最初の要素をデータベースから探します。
        // 見つからない場合は null を返します。
        return await _dbContext.Todos.FirstOrDefaultAsync(todo => todo.Id == id);
    }

    public async Task<TodoItem> CreateAsync(CreateTodoRequest request)
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
        await _dbContext.SaveChangesAsync();

        // {TodoId}は構造化ログのプレースホルダーです。
        // タイトル本文はログに出さず、操作とIDだけを記録します。
        _logger.LogInformation("Created todo with id {TodoId}", todo.Id);

        return todo;
    }

    public async Task<TodoItem?> UpdateAsync(int id, UpdateTodoRequest request)
    {
        var existingTodo = await GetByIdAsync(id);

        if (existingTodo is null)
        {
            _logger.LogWarning("Todo with id {TodoId} was not found for update", id);
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

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated todo with id {TodoId}", id);

        return existingTodo;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var todo = await GetByIdAsync(id);

        if (todo is null)
        {
            _logger.LogWarning("Todo with id {TodoId} was not found for delete", id);
            return false;
        }

        _dbContext.Todos.Remove(todo);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted todo with id {TodoId}", id);

        return true;
    }
}
