using Microsoft.EntityFrameworkCore;

// EfTodoRepositoryは、ITodoRepositoryをEntity Framework Coreで実装します。
// SQLへ変換されるIQueryableの組み立ては、このInfrastructure層に閉じ込めます。
public sealed class EfTodoRepository : ITodoRepository
{
    private readonly TodoDbContext _dbContext;

    public EfTodoRepository(TodoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Todos.FirstOrDefaultAsync(
            todo => todo.Id == id,
            cancellationToken
        );
    }

    public async Task<TodoPageResult> GetPageAsync(
        TodoListQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = BuildFilteredQuery(request.IsDone, request.Search);
        var totalCount = await query.CountAsync(cancellationToken);

        var normalizedSortBy = (request.SortBy ?? TodoSortValidation.DefaultSortBy)
            .Trim()
            .ToLowerInvariant();
        var normalizedSortOrder = (request.SortOrder ?? TodoSortValidation.DefaultSortOrder)
            .Trim()
            .ToLowerInvariant();

        query = normalizedSortBy switch
        {
            "title" when normalizedSortOrder == "desc" => query
                .OrderByDescending(todo => todo.Title)
                .ThenByDescending(todo => todo.Id),
            "title" => query.OrderBy(todo => todo.Title).ThenBy(todo => todo.Id),
            "createdat" when normalizedSortOrder == "desc" => query
                .OrderByDescending(todo => todo.CreatedAt)
                .ThenByDescending(todo => todo.Id),
            "createdat" => query.OrderBy(todo => todo.CreatedAt).ThenBy(todo => todo.Id),
            "id" when normalizedSortOrder == "desc" => query.OrderByDescending(todo => todo.Id),
            _ => query.OrderBy(todo => todo.Id)
        };

        var todos = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new TodoPageResult(
            Items: todos,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }

    public async Task<TodoCursorPageResult> GetCursorPageAsync(
        TodoCursorQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = BuildFilteredQuery(request.IsDone, request.Search);

        if (request.AfterId.HasValue)
        {
            query = query.Where(todo => todo.Id > request.AfterId.Value);
        }

        var todos = await query
            .OrderBy(todo => todo.Id)
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = todos.Count > request.PageSize;
        if (hasNextPage)
        {
            todos.RemoveAt(todos.Count - 1);
        }

        return new TodoCursorPageResult(
            Items: todos,
            PageSize: request.PageSize,
            LastTodoId: todos.Count > 0 ? todos[^1].Id : null,
            HasNextPage: hasNextPage
        );
    }

    public void Add(TodoItem todo) => _dbContext.Todos.Add(todo);

    public void Remove(TodoItem todo) => _dbContext.Todos.Remove(todo);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TodoItem> BuildFilteredQuery(bool? isDone, string? search)
    {
        var specification = new TodoFilterSpecification(isDone, search);
        return _dbContext.Todos.AsNoTracking().Where(specification.Criteria);
    }
}
