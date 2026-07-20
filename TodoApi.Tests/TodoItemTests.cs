namespace TodoApi.Tests;

public class TodoItemTests
{
    [Fact]
    public void Create_WithValidTitle_ReturnsIncompleteTodo()
    {
        var createdAt = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero);

        var result = TodoItem.Create("Learn DDD", createdAt);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Learn DDD", result.Value.Title);
        Assert.False(result.Value.IsDone);
        Assert.Null(result.Value.CompletedAt);
        Assert.Equal(createdAt, result.Value.CreatedAt);
    }

    [Fact]
    public void Create_WithBlankTitle_ReturnsDomainError()
    {
        var result = TodoItem.Create(" ", DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("title_required", result.Error?.Code);
    }

    [Fact]
    public void ChangeTitle_WithTooLongTitle_ReturnsDomainError()
    {
        var todo = TodoItem.Create("Initial", DateTimeOffset.UtcNow).Value!;

        var result = todo.ChangeTitle(new string('a', TodoRules.MaxTitleLength + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("title_too_long", result.Error?.Code);
        Assert.Equal("Initial", todo.Title);
    }

    [Fact]
    public void Complete_PreservesTheFirstCompletionTime()
    {
        var firstCompletedAt = new DateTimeOffset(2026, 7, 20, 1, 0, 0, TimeSpan.Zero);
        var secondCompletedAt = firstCompletedAt.AddHours(1);
        var todo = TodoItem.Create("Complete me", DateTimeOffset.UtcNow).Value!;

        todo.Complete(firstCompletedAt);
        todo.Complete(secondCompletedAt);

        Assert.True(todo.IsDone);
        Assert.Equal(firstCompletedAt, todo.CompletedAt);
    }

    [Fact]
    public void Reopen_ClearsCompletionState()
    {
        var todo = TodoItem.Create("Reopen me", DateTimeOffset.UtcNow).Value!;
        var completedAt = new DateTimeOffset(2026, 7, 20, 2, 0, 0, TimeSpan.Zero);

        todo.Complete(completedAt);
        todo.Reopen();

        Assert.False(todo.IsDone);
        Assert.Null(todo.CompletedAt);
    }
}
