using Microsoft.EntityFrameworkCore;

// WebApplication.CreateBuilder は、ASP.NET Coreアプリを作るための準備をします。
// args には、コマンドライン引数が入ります。今は特別な引数を使っていません。
var builder = WebApplication.CreateBuilder(args);

// AddDbContext は、Entity Framework Coreで使うDbContextをDIコンテナに登録します。
// ConnectionStrings:TodoDatabase は appsettings.json に書いたSQLiteの接続先です。
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TodoDatabase")));

// TodoServiceは、Todoの作成・取得・更新・削除の処理をまとめたサービスです。
// AddScoped は、HTTPリクエストごとに1つのインスタンスを作る登録方法です。
builder.Services.AddScoped<TodoService>();

// Build を呼ぶと、実際に起動できるWebアプリケーションの本体が作られます。
var app = builder.Build();

// Migrate は、未適用のマイグレーションをデータベースへ反映します。
// 今回はハンズオンを簡単にするため、起動時にSQLiteのテーブルを自動作成します。
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    dbContext.Database.Migrate();
}

// GET / にアクセスされたときの処理です。
// () => ... はラムダ式で、「引数なしで、この値を返す処理」を短く書いています。
app.MapGet("/", () => "Todo API is running.");

// GET /todos は、Todo一覧を返します。
// Results.Ok はHTTP 200 OKのレスポンスを作ります。
app.MapGet("/todos", async (TodoService todoService) =>
{
    var todos = await todoService.GetAllAsync();

    return Results.Ok(todos);
});

// GET /todos/1 のように、URLの一部からidを受け取ります。
// {id:int} と書くことで、idは整数だけ受け付けます。
app.MapGet("/todos/{id:int}", async (int id, TodoService todoService) =>
{
    var todo = await todoService.GetByIdAsync(id);

    // 三項演算子です。
    // 条件 ? trueの場合の値 : falseの場合の値 という形で書きます。
    return todo is null
        ? Results.NotFound()
        : Results.Ok(todo);
});

// POST /todos は、新しいTodoを作成します。
// リクエストボディのJSONは、CreateTodoRequest型として受け取れます。
app.MapPost("/todos", async (CreateTodoRequest request, TodoService todoService) =>
{
    var validation = TodoValidation.ValidateTitle(request.Title);

    if (!validation.IsValid)
    {
        return Results.BadRequest(validation.Error);
    }

    var todo = await todoService.CreateAsync(request);

    // Created はHTTP 201 Createdを返します。
    // 第1引数には、作成されたリソースのURLを入れます。
    return Results.Created($"/todos/{todo.Id}", todo);
});

// PUT /todos/1 は、指定したTodoを更新します。
// UpdateTodoRequestでは Title と IsDone を nullable にしているので、片方だけ更新できます。
app.MapPut("/todos/{id:int}", async (int id, UpdateTodoRequest request, TodoService todoService) =>
{
    var validation = TodoValidation.ValidateOptionalTitle(request.Title);

    if (!validation.IsValid)
    {
        return Results.BadRequest(validation.Error);
    }

    var updatedTodo = await todoService.UpdateAsync(id, request);

    if (updatedTodo is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(updatedTodo);
});

// DELETE /todos/1 は、指定したTodoを削除します。
app.MapDelete("/todos/{id:int}", async (int id, TodoService todoService) =>
{
    var deleted = await todoService.DeleteAsync(id);

    if (!deleted)
    {
        return Results.NotFound();
    }

    // NoContent はHTTP 204 No Contentを返します。
    // 削除成功時は、返すデータがないので204を使います。
    return Results.NoContent();
});

// アプリを起動して、HTTPリクエストを待ち受けます。
app.Run();

// テストプロジェクトから、このMinimal APIアプリを起動できるようにするための型です。
// partial は「同じクラスの定義を別の場所にも分けて書ける」という意味です。
public partial class Program;
