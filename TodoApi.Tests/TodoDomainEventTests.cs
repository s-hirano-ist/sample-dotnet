namespace TodoApi.Tests;

public class TodoDomainEventTests
{
    [Fact]
    public void Complete_AddsAnEventOnlyWhenStateChanges()
    {
        var todo = TodoItem.Create("Complete", DateTimeOffset.UtcNow).Value!;
        var completedAt = new DateTimeOffset(2026, 7, 20, 3, 0, 0, TimeSpan.Zero);

        todo.Complete(completedAt);
        todo.Complete(completedAt.AddMinutes(1));

        var domainEvents = todo.DequeueDomainEvents();

        var completedEvent = Assert.Single(domainEvents);
        var typedEvent = Assert.IsType<TodoCompletedDomainEvent>(completedEvent);
        Assert.Equal(completedAt, typedEvent.CompletedAt);
        Assert.Empty(todo.DomainEvents);
    }

    [Fact]
    public void ChangeTitle_AddsAnEventOnlyWhenValueChanges()
    {
        var todo = TodoItem.Create("Initial", DateTimeOffset.UtcNow).Value!;

        todo.ChangeTitle("Initial");
        todo.ChangeTitle("Changed");

        var domainEvents = todo.DequeueDomainEvents();

        var titleChangedEvent = Assert.Single(domainEvents);
        Assert.IsType<TodoTitleChangedDomainEvent>(titleChangedEvent);
    }

    [Fact]
    public void Reopen_AddsAnEventOnlyWhenTodoWasCompleted()
    {
        var todo = TodoItem.Create("Reopen", DateTimeOffset.UtcNow).Value!;

        todo.Reopen();
        todo.Complete(DateTimeOffset.UtcNow);
        todo.Reopen();

        var domainEvents = todo.DequeueDomainEvents();

        Assert.Collection(
            domainEvents,
            domainEvent => Assert.IsType<TodoCompletedDomainEvent>(domainEvent),
            domainEvent => Assert.IsType<TodoReopenedDomainEvent>(domainEvent)
        );
    }
}
