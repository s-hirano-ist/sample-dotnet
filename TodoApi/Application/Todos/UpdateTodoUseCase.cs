// UpdateTodoUseCaseは、Todo更新の一連の処理を担当します。
public partial class UpdateTodoUseCase
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<UpdateTodoUseCase> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public UpdateTodoUseCase(
        ITodoRepository repository,
        ILogger<UpdateTodoUseCase> logger,
        TimeProvider timeProvider,
        IDomainEventDispatcher domainEventDispatcher
    )
    {
        _repository = repository;
        _logger = logger;
        _timeProvider = timeProvider;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<TodoItem?> ExecuteAsync(
        int id,
        UpdateTodoRequest request,
        CancellationToken cancellationToken
    )
    {
        var todo = await _repository.GetByIdAsync(id, cancellationToken);

        if (todo is null)
        {
            LogTodoUpdateNotFound(id);
            return null;
        }

        if (request.Title is not null)
        {
            RequireSuccess(todo.ChangeTitle(request.Title));
        }

        if (request.IsDone is true)
        {
            RequireSuccess(todo.Complete(_timeProvider.GetUtcNow()));
        }
        else if (request.IsDone is false)
        {
            RequireSuccess(todo.Reopen());
        }

        await _repository.SaveChangesAsync(cancellationToken);

        await _domainEventDispatcher.DispatchAsync(
            todo.DequeueDomainEvents(),
            cancellationToken
        );

        LogTodoUpdated(id);
        return todo;
    }

    [LoggerMessage(EventId = ApiLogEvents.TodoUpdatedId, Level = LogLevel.Information, Message = "Updated todo with id {TodoId}")]
    private partial void LogTodoUpdated(int todoId);

    [LoggerMessage(EventId = ApiLogEvents.TodoUpdateNotFoundId, Level = LogLevel.Warning, Message = "Todo with id {TodoId} was not found for update")]
    private partial void LogTodoUpdateNotFound(int todoId);

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
