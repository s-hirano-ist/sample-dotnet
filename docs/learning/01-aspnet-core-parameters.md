# ASP.NET Coreの引数を理解する

この資料では、エンドポイントの引数に値が入る仕組みを、普通のC#から順番に確認します。

## 1. 普通のC#メソッド

まず、ASP.NET Coreがない普通のC#メソッドを考えます。

```csharp
static string CreateMessage(string name)
{
    return $"Hello, {name}!";
}

var message = CreateMessage("Alice");
```

`CreateMessage`には`name`という引数があります。

```csharp
CreateMessage("Alice");
```

このように、メソッドを呼び出す側が`"Alice"`を明示的に渡します。

ここでは、値を用意しているのはC#を書いた自分です。

## 2. Minimal APIのエンドポイント

Minimal APIでは、ラムダ式をHTTPリクエストの処理として登録します。

```csharp
app.MapGet("/hello", () => "Hello!");
```

`MapGet`は、次のような処理を登録しています。

```text
GET /hello が届いたら、() => "Hello!" を実行する
```

ラムダ式の`()`は「引数がない」という意味です。

## 3. URLの値を受け取る

URLに`{id}`を書いて、ラムダ式に`int id`を追加します。

```csharp
app.MapGet("/todos/{id}", (int id) =>
{
    return $"Todo ID: {id}";
});
```

次のリクエストを送るとします。

```text
GET /todos/123
```

ASP.NET CoreはURLの`123`を読み取り、`id`へ渡してからラムダ式を実行します。

```text
HTTPのURL: /todos/123
       ↓
int id: 123
       ↓
ラムダ式を実行
```

`Program.cs`にある次のコードも同じ仕組みです。

```csharp
app.MapGet("/todos/{id:int}", async (
    int id,
    TodoService todoService,
    CancellationToken cancellationToken
) =>
{
    var todo = await todoService.GetByIdAsync(id, cancellationToken);
    // ...
});
```

`{id:int}`は、URLの値が整数であることも指定しています。

## 4. クエリ文字列を受け取る

URLの`?`以降はクエリ文字列です。

```text
GET /todos?page=2&pageSize=10
```

エンドポイントでは、次のように受け取れます。

```csharp
app.MapGet("/todos", (int? page, int? pageSize) =>
{
    var currentPage = page ?? 1;
    var currentPageSize = pageSize ?? 20;

    return $"page={currentPage}, pageSize={currentPageSize}";
});
```

対応関係は次の通りです。

```text
?page=2       -> int? page       -> 2
&pageSize=10  -> int? pageSize   -> 10
```

`int?`はnullableな`int`です。クエリ文字列がない場合は`null`になるため、`??`でデフォルト値を設定しています。

## 5. JSONボディを受け取る

POSTやPUTでは、JSONボディをC#の型へ変換できます。

```csharp
public record CreateTodoRequest(string Title);

app.MapPost("/todos", (CreateTodoRequest request) =>
{
    return request.Title;
});
```

次のJSONを送ります。

```json
{
  "title": "Learn .NET"
}
```

ASP.NET CoreがJSONを読み取り、`CreateTodoRequest`を作成します。

```text
JSONのtitle: "Learn .NET"
        ↓
CreateTodoRequest.Title
        ↓
request.Title
```

これはC#の通常のメソッド呼び出しとは違い、ASP.NET CoreがHTTPボディを解析して値を用意しています。

## 6. DIからサービスを受け取る

`TodoService`は、URLやJSONから来る値ではありません。DIコンテナに登録したサービスです。

```csharp
builder.Services.AddScoped<TodoService>();
```

この登録によって、ASP.NET Coreは`TodoService`を作成して管理できるようになります。

エンドポイントに次の引数を書くと、DIコンテナから取得して渡します。

```csharp
app.MapGet("/todos", (TodoService todoService) =>
{
    return todoService.GetPageAsync(/* ... */);
});
```

普通のC#なら、次のように自分でインスタンスを渡します。

```csharp
var service = new TodoService(/* 必要な引数 */);
DoSomething(service);
```

Minimal APIでは、ASP.NET Coreがこの「サービスを探して渡す」処理を行います。

## 7. 特別に用意される型

ASP.NET Coreは、いくつかの型をHTTPコンテキストから用意します。

| 引数 | 値の取得元 |
| --- | --- |
| `int id` | URLのルートパラメータ |
| `string? search` | クエリ文字列 |
| `CreateTodoRequest request` | JSONリクエストボディ |
| `TodoService service` | DIコンテナ |
| `HttpContext context` | 現在のHTTPリクエスト |
| `CancellationToken token` | `HttpContext.RequestAborted` |

これはC#の予約語による機能ではありません。ASP.NET Coreがエンドポイントの引数を調べ、型や名前に応じて値を用意しています。

## 練習問題

次の対応関係を説明してみてください。

```csharp
app.MapGet("/todos/{id:int}", async (
    int id,
    string? search,
    TodoService todoService,
    CancellationToken cancellationToken
) =>
{
    // 処理
});
```

```text
GET /todos/5?search=learn
```

確認するポイント:

- `id`には何が入るか
- `search`には何が入るか
- `todoService`はどこから来るか
- `cancellationToken`は何を表すか

答えは、次のようになります。

```text
id                 -> URLの5
search             -> クエリ文字列のlearn
todoService        -> DIコンテナ
cancellationToken  -> リクエストの中断通知
```
