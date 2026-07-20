namespace TodoApi.Tests;

public class TodoFilterSpecificationTests
{
    [Fact]
    public void IsDoneFilter_MatchesOnlyTheRequestedState()
    {
        var specification = new TodoFilterSpecification(true, null);
        var predicate = specification.Criteria.Compile();

        var doneTodo = new TodoItem(1, "Done", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var openTodo = new TodoItem(2, "Open", false, DateTimeOffset.UtcNow, null);

        Assert.True(predicate(doneTodo));
        Assert.False(predicate(openTodo));
    }

    [Fact]
    public void SearchFilter_IgnoresCaseAndOuterWhitespace()
    {
        var specification = new TodoFilterSpecification(null, "  learn  ");
        var predicate = specification.Criteria.Compile();

        var matchingTodo = new TodoItem(1, "Learn DDD", true, DateTimeOffset.UtcNow, null);
        var otherTodo = new TodoItem(2, "Read a book", false, DateTimeOffset.UtcNow, null);

        Assert.True(predicate(matchingTodo));
        Assert.False(predicate(otherTodo));
    }

    [Fact]
    public void EmptyFilter_MatchesEveryTodo()
    {
        var specification = new TodoFilterSpecification(null, null);
        var predicate = specification.Criteria.Compile();

        var todo = new TodoItem(1, "Any title", false, DateTimeOffset.UtcNow, null);

        Assert.True(predicate(todo));
    }
}
