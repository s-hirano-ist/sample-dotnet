// CreateTodoUseCaseは、Todo作成の一連の処理を担当します。
public partial class CreateTodoUseCase
{
    private readonly ITodoRepository _repository;
    private readonly ILogger<CreateTodoUseCase> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public CreateTodoUseCase(
        ITodoRepository repository,
        ILogger<CreateTodoUseCase> logger,
        TimeProvider timeProvider,
        IDomainEventDispatcher domainEventDispatcher
    )
    {
        _repository = repository;
        _logger = logger;
        _timeProvider = timeProvider;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<TodoItem> ExecuteAsync(
        CreateTodoRequest request,
        CancellationToken cancellationToken
    )
    {
        var creation = TodoItem.Create(request.Title, _timeProvider.GetUtcNow());
        var todo = RequireValue(creation);

        _repository.Add(todo);
        await _repository.SaveChangesAsync(cancellationToken);

        todo.AddDomainEvent(new TodoCreatedDomainEvent(todo.Id));
        await _domainEventDispatcher.DispatchAsync(
            todo.DequeueDomainEvents(),
            cancellationToken
        );

        LogTodoCreated(todo.Id);
        return todo;
    }

    [LoggerMessage(EventId = ApiLogEvents.TodoCreatedId, Level = LogLevel.Information, Message = "Created todo with id {TodoId}")]
    private partial void LogTodoCreated(int todoId);

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
}
