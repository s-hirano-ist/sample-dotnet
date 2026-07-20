// DeleteTodoUseCaseは、Todo削除の一連の処理を担当します。
public partial class DeleteTodoUseCase
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<DeleteTodoUseCase> _logger;

    public DeleteTodoUseCase(
        ITodoRepository repository,
        ILogger<DeleteTodoUseCase> logger
    )
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(int id, CancellationToken cancellationToken)
    {
        var todo = await _repository.GetByIdAsync(id, cancellationToken);

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

    [LoggerMessage(EventId = ApiLogEvents.TodoDeletedId, Level = LogLevel.Information, Message = "Deleted todo with id {TodoId}")]
    private partial void LogTodoDeleted(int todoId);

    [LoggerMessage(EventId = ApiLogEvents.TodoDeleteNotFoundId, Level = LogLevel.Warning, Message = "Todo with id {TodoId} was not found for delete")]
    private partial void LogTodoDeleteNotFound(int todoId);
}
