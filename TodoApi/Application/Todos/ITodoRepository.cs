// ITodoRepositoryは、TodoServiceが必要とする保存処理の契約です。
// EF CoreやPostgreSQLなどの具体的な仕組みをApplication層から隠します。
public interface ITodoRepository
{
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<TodoPageResult> GetPageAsync(
        TodoListQuery query,
        CancellationToken cancellationToken
    );

    Task<TodoCursorPageResult> GetCursorPageAsync(
        TodoCursorQuery query,
        CancellationToken cancellationToken
    );

    void Add(TodoItem todo);

    void Remove(TodoItem todo);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
