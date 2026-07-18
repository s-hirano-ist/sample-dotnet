# DI（依存性注入）を理解する

DIはDependency Injectionの略です。日本語では「依存性注入」と呼びます。

まず、DIを使わない普通のC#から確認します。

## 1. `new`でオブジェクトを作る

```csharp
public class GreetingService
{
    public string SayHello()
    {
        return "Hello!";
    }
}

var service = new GreetingService();
var message = service.SayHello();
```

`new GreetingService()`で、自分で`GreetingService`のインスタンスを作っています。

この方法は小さなプログラムでは分かりやすい一方、クラスが必要とするものが増えると呼び出し側の責務も増えます。

```csharp
var dbContext = new TodoDbContext(/* DB設定 */);
var logger = /* Loggerを自分で作る */;
var todoService = new TodoService(dbContext, logger);
```

`TodoService`を使う側が、DBやログの準備方法まで知る必要があります。

## 2. コンストラクタで依存するものを受け取る

`TodoService`はコンストラクタで必要なものを受け取ります。

```csharp
public TodoService(
    TodoDbContext dbContext,
    ILogger<TodoService> logger
)
{
    _dbContext = dbContext;
    _logger = logger;
}
```

`TodoService`が自分で`TodoDbContext`を作らず、外部から受け取っている点が重要です。

このように、クラスが必要とするオブジェクトを「依存」と呼びます。

```text
TodoService
  -> TodoDbContextに依存
  -> ILogger<TodoService>に依存
```

## 3. DIコンテナへ登録する

ASP.NET Coreでは、必要なオブジェクトをDIコンテナに登録します。

```csharp
builder.Services.AddScoped<TodoService>();
```

このコードは、次のような意味です。

```text
TodoServiceが必要になったら、DIコンテナが作成して渡せるようにする
```

`TodoService`自身の依存先も登録されていれば、DIコンテナが順番に解決します。

```text
TodoServiceが必要
  ↓
TodoDbContextを用意
ILogger<TodoService>を用意
  ↓
TodoServiceのコンストラクタへ渡す
```

## 4. エンドポイントで使う

エンドポイントの引数に`TodoService`を書くと、ASP.NET CoreがDIコンテナから取得して渡します。

```csharp
app.MapGet("/todos", async (TodoService todoService) =>
{
    return await todoService.GetPageAsync(
        page: 1,
        pageSize: 20,
        isDone: null,
        search: null,
        sortBy: "id",
        sortOrder: "asc",
        cancellationToken: default
    );
});
```

ここで、エンドポイントは`new TodoService(...)`を書いていません。

```text
エンドポイント
  -> TodoServiceを要求
ASP.NET Core
  -> DIコンテナからTodoServiceを取得
  -> 引数へ渡す
```

## 5. `AddScoped`の意味

`AddScoped`は、DIサービスの生存期間を指定する登録方法です。

```csharp
builder.Services.AddScoped<TodoService>();
```

主な登録方法は次の3つです。

| 登録方法 | インスタンスの単位 | 主な用途 |
| --- | --- | --- |
| `AddTransient` | 依頼されるたびに新しく作る | 軽量で状態を持たない処理 |
| `AddScoped` | HTTPリクエストごとに1つ | DbContext、業務サービス |
| `AddSingleton` | アプリ起動中に1つ | 共有設定、接続管理 |

Todo APIでは、`TodoDbContext`と`TodoService`をリクエスト単位で扱うため、`Scoped`が基本になります。

## 6. DIを使うメリット

DIを使うと、クラスが具体的な作成方法に強く依存しなくなります。

```csharp
public class TodoService
{
    private readonly TodoDbContext _dbContext;

    public TodoService(TodoDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
```

`TodoService`は「DB設定をどう作るか」ではなく、「TodoDbContextを使う」ことだけを知っています。

テストでは、別のDB設定や偽物のサービスを渡すこともできます。これにより、クラスを単独でテストしやすくなります。

## 練習問題

次のコードを読んで、DIコンテナが何をするか説明してみてください。

```csharp
builder.Services.AddScoped<TodoService>();
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite("Data Source=todo.db")
);

app.MapGet("/todos", (TodoService service) =>
{
    // serviceを使う
});
```

確認するポイント:

- `TodoService`はどこで登録されているか
- `TodoService`のコンストラクタへ何が渡されるか
- エンドポイントで`new TodoService()`を書かなくてよい理由
- `AddScoped`でインスタンスが作られる単位
