using Microsoft.Extensions.Options;

namespace TodoApi.Tests;

public class TodoIdempotencyStoreTests
{
    [Fact]
    public async Task ExecuteAsync_BeforeExpiry_ReusesOriginalResult()
    {
        var clock = new TestTimeProvider();
        var store = CreateStore(clock, lifetimeSeconds: 60);
        var executions = 0;

        var first = await store.ExecuteAsync(
            "client",
            "key",
            "request-a",
            () =>
            {
                executions++;
                return Task.FromResult(CreateTodo(1));
            },
            CancellationToken.None
        );
        var replay = await store.ExecuteAsync(
            "client",
            "key",
            "request-a",
            () =>
            {
                executions++;
                return Task.FromResult(CreateTodo(2));
            },
            CancellationToken.None
        );

        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(1, replay.Todo?.Id);
        Assert.Equal(1, executions);
    }

    [Fact]
    public async Task ExecuteAsync_AfterExpiry_AllowsNewExecution()
    {
        var clock = new TestTimeProvider();
        var store = CreateStore(clock, lifetimeSeconds: 60);

        var first = await store.ExecuteAsync(
            "client",
            "key",
            "request-a",
            () => Task.FromResult(CreateTodo(1)),
            CancellationToken.None
        );

        clock.Advance(TimeSpan.FromSeconds(61));

        var second = await store.ExecuteAsync(
            "client",
            "key",
            "request-a",
            () => Task.FromResult(CreateTodo(2)),
            CancellationToken.None
        );

        Assert.False(first.IsReplay);
        Assert.False(second.IsReplay);
        Assert.Equal(2, second.Todo?.Id);
    }

    private static TodoIdempotencyStore CreateStore(TestTimeProvider clock, int lifetimeSeconds)
    {
        return new TodoIdempotencyStore(
            Options.Create(new IdempotencyOptions { EntryLifetimeSeconds = lifetimeSeconds }),
            clock
        );
    }

    private static TodoItem CreateTodo(int id)
    {
        return new TodoItem(id, $"Todo {id}", false, DateTimeOffset.UtcNow, null);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
