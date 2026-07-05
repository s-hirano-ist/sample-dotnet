// WebApplication.CreateBuilder は、ASP.NET Coreアプリを作るための準備をします。
// args には、コマンドライン引数が入ります。今は特別な引数を使っていません。
var builder = WebApplication.CreateBuilder(args);

// Build を呼ぶと、実際に起動できるWebアプリケーションの本体が作られます。
var app = builder.Build();

// 今回は学習用なので、データベースではなくメモリ上のListにTodoを保存します。
// アプリを止めると、このListの中身は消えます。
var todos = new List<TodoItem>();

// 新しいTodoに割り当てるIDです。
// nextId++ と書くと、今の値を使った後に1増えます。
var nextId = 1;

// GET / にアクセスされたときの処理です。
// () => ... はラムダ式で、「引数なしで、この値を返す処理」を短く書いています。
app.MapGet("/", () => "Todo API is running.");

// GET /todos は、Todo一覧を返します。
// Results.Ok はHTTP 200 OKのレスポンスを作ります。
app.MapGet("/todos", () =>
{
    return Results.Ok(todos);
});

// GET /todos/1 のように、URLの一部からidを受け取ります。
// {id:int} と書くことで、idは整数だけ受け付けます。
app.MapGet("/todos/{id:int}", (int id) =>
{
    // FirstOrDefault は、条件に合う最初の要素を探します。
    // 見つからない場合は null を返します。
    var todo = todos.FirstOrDefault(todo => todo.Id == id);

    // 三項演算子です。
    // 条件 ? trueの場合の値 : falseの場合の値 という形で書きます。
    return todo is null
        ? Results.NotFound()
        : Results.Ok(todo);
});

// POST /todos は、新しいTodoを作成します。
// リクエストボディのJSONは、CreateTodoRequest型として受け取れます。
app.MapPost("/todos", (CreateTodoRequest request) =>
{
    // string.IsNullOrWhiteSpace は、null・空文字・空白だけの文字列をチェックします。
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest("Title is required.");
    }

    // record型は、new TodoItem(...) のように値を指定して作れます。
    var todo = new TodoItem(
        Id: nextId++,
        Title: request.Title,
        IsDone: false,
        CreatedAt: DateTimeOffset.UtcNow,
        CompletedAt: null
    );

    todos.Add(todo);

    // Created はHTTP 201 Createdを返します。
    // 第1引数には、作成されたリソースのURLを入れます。
    return Results.Created($"/todos/{todo.Id}", todo);
});

// PUT /todos/1 は、指定したTodoを更新します。
// UpdateTodoRequestでは Title と IsDone を nullable にしているので、片方だけ更新できます。
app.MapPut("/todos/{id:int}", (int id, UpdateTodoRequest request) =>
{
    // FindIndex は、条件に合う要素の位置を返します。
    // 見つからない場合は -1 を返します。
    var index = todos.FindIndex(todo => todo.Id == id);

    if (index == -1)
    {
        return Results.NotFound();
    }

    var existingTodo = todos[index];

    // ?? は null合体演算子です。
    // 左側がnullでなければ左側、nullなら右側を使います。
    var isDone = request.IsDone ?? existingTodo.IsDone;

    // recordの with 式です。
    // existingTodoを元に、一部のプロパティだけ変えた新しいTodoを作ります。
    var updatedTodo = existingTodo with
    {
        Title = request.Title ?? existingTodo.Title,
        IsDone = isDone,
        // 完了状態なら完了日時を入れ、未完了なら null に戻します。
        CompletedAt = isDone
            ? existingTodo.CompletedAt ?? DateTimeOffset.UtcNow
            : null
    };

    // List内の古いTodoを、新しく作ったTodoで置き換えます。
    todos[index] = updatedTodo;

    return Results.Ok(updatedTodo);
});

// DELETE /todos/1 は、指定したTodoを削除します。
app.MapDelete("/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(todo => todo.Id == id);

    if (todo is null)
    {
        return Results.NotFound();
    }

    todos.Remove(todo);

    // NoContent はHTTP 204 No Contentを返します。
    // 削除成功時は、返すデータがないので204を使います。
    return Results.NoContent();
});

// アプリを起動して、HTTPリクエストを待ち受けます。
app.Run();

// TodoItemは、APIの中で扱うTodoのデータ構造です。
// recordは「値を表す型」を短く書けるC#の構文です。
record TodoItem(
    int Id,
    string Title,
    bool IsDone,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);

// Todo作成時に受け取るJSONの形です。
// 例: { "title": "Learn .NET" }
record CreateTodoRequest(string Title);

// Todo更新時に受け取るJSONの形です。
// string? や bool? の ? は、nullを許可するという意味です。
// 例: { "isDone": true }
record UpdateTodoRequest(
    string? Title,
    bool? IsDone
);
