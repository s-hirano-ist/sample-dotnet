var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<TodoItem>();
var nextId = 1;

app.MapGet("/", () => "Todo API is running.");

app.MapGet("/todos", () =>
{
    return Results.Ok(todos);
});

app.MapGet("/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(todo => todo.Id == id);

    return todo is null
        ? Results.NotFound()
        : Results.Ok(todo);
});

app.MapPost("/todos", (CreateTodoRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest("Title is required.");
    }

    var todo = new TodoItem(
        Id: nextId++,
        Title: request.Title,
        IsDone: false,
        CreatedAt: DateTimeOffset.UtcNow,
        CompletedAt: null
    );

    todos.Add(todo);

    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id:int}", (int id, UpdateTodoRequest request) =>
{
    var index = todos.FindIndex(todo => todo.Id == id);

    if (index == -1)
    {
        return Results.NotFound();
    }

    var existingTodo = todos[index];
    var isDone = request.IsDone ?? existingTodo.IsDone;

    var updatedTodo = existingTodo with
    {
        Title = request.Title ?? existingTodo.Title,
        IsDone = isDone,
        CompletedAt = isDone
            ? existingTodo.CompletedAt ?? DateTimeOffset.UtcNow
            : null
    };

    todos[index] = updatedTodo;

    return Results.Ok(updatedTodo);
});

app.MapDelete("/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(todo => todo.Id == id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    todos.Remove(todo);

    return Results.NoContent();
});

app.Run();

record TodoItem(
    int Id,
    string Title,
    bool IsDone,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);

record CreateTodoRequest(string Title);

record UpdateTodoRequest(
    string? Title,
    bool? IsDone
);
