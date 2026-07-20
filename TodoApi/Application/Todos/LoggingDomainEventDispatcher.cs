// LoggingDomainEventDispatcherは、Domain Eventを構造化ログへ記録します。
// 将来は監査ログ、通知、メッセージキューなどのHandlerへ置き換えられます。
public partial class LoggingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<LoggingDomainEventDispatcher> _logger;

    public LoggingDomainEventDispatcher(ILogger<LoggingDomainEventDispatcher> logger)
    {
        _logger = logger;
    }

    public Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken
    )
    {
        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (domainEvent)
            {
                case TodoCreatedDomainEvent created:
                    LogTodoCreated(created.TodoId);
                    break;
                case TodoTitleChangedDomainEvent titleChanged:
                    LogTodoTitleChanged(titleChanged.TodoId);
                    break;
                case TodoCompletedDomainEvent completed:
                    LogTodoCompleted(completed.TodoId, completed.CompletedAt);
                    break;
                case TodoReopenedDomainEvent reopened:
                    LogTodoReopened(reopened.TodoId);
                    break;
                case TodoDeletedDomainEvent deleted:
                    LogTodoDeleted(deleted.TodoId);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Domain event TodoCreated for todo {TodoId}")]
    private partial void LogTodoCreated(int todoId);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Domain event TodoTitleChanged for todo {TodoId}")]
    private partial void LogTodoTitleChanged(int todoId);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Domain event TodoCompleted for todo {TodoId} at {CompletedAt}")]
    private partial void LogTodoCompleted(int todoId, DateTimeOffset completedAt);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Domain event TodoReopened for todo {TodoId}")]
    private partial void LogTodoReopened(int todoId);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "Domain event TodoDeleted for todo {TodoId}")]
    private partial void LogTodoDeleted(int todoId);
}
