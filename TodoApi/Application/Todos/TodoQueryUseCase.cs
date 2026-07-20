// TodoQueryUseCaseは、Todoを読み取るユースケースをまとめます。
// HTTPやEF Coreの詳細ではなく、読み取りに必要なApplicationの流れを担当します。
public sealed class TodoQueryUseCase
{
    private readonly ITodoRepository _repository;

    public TodoQueryUseCase(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<TodoListResponse> ListAsync(
        TodoListQuery query,
        CancellationToken cancellationToken
    )
    {
        var result = await _repository.GetPageAsync(query, cancellationToken);

        return new TodoListResponse(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages
        );
    }

    public async Task<TodoCursorListResponse> ListByCursorAsync(
        TodoCursorQuery query,
        CancellationToken cancellationToken
    )
    {
        var result = await _repository.GetCursorPageAsync(query, cancellationToken);

        var nextCursor = result.HasNextPage && result.LastTodoId.HasValue
            ? TodoCursor.Create(result.LastTodoId.Value)
            : null;

        return new TodoCursorListResponse(
            Items: result.Items,
            PageSize: query.PageSize,
            NextCursor: nextCursor,
            HasNextPage: result.HasNextPage
        );
    }

    public Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }
}
