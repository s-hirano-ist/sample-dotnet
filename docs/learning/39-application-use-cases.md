# Application Use Case分離

## 今回の目的

HTTPエンドポイントに書かれていたTodo操作を、Application層のUse Caseへ分けました。

Use Caseは「利用者が行う一つの操作」を表します。今回のTodo APIでは、読み取り、作成、更新、削除をそれぞれApplicationのクラスへ分離しています。

## 1. HTTP入口の責務

`Program.cs`のエンドポイントは、主に次の処理を担当します。

- URL、クエリ、JSONから値を受け取る
- HTTP入力を検証する
- 認証、認可、ETag、冪等性キーを扱う
- Use Caseを呼び出す
- 結果をHTTPステータスとJSONへ変換する

一方、Todoの作成や更新の手順はUse Caseへ移しました。

## 2. Use Caseの種類

今回の構成は次の通りです。

- `TodoQueryUseCase`: 一覧、カーソル一覧、1件取得
- `CreateTodoUseCase`: Todo作成と保存
- `UpdateTodoUseCase`: Entityの更新と保存
- `DeleteTodoUseCase`: Todo削除と保存

読み取りをQuery、状態を変更する処理をCommandとして分けています。これはCQRSを完全に導入したわけではなく、責務を読み取りと書き込みで整理する第一歩です。

## 3. DIでUse Caseを渡す

`Program.cs`でUse CaseをDIへ登録します。

```csharp
builder.Services.AddScoped<CreateTodoUseCase>();
```

エンドポイントの引数に型を書くと、ASP.NET CoreがDIコンテナからインスタンスを渡します。

```csharp
app.MapPost("/todos", async (
    CreateTodoRequest request,
    CreateTodoUseCase createTodoUseCase,
    CancellationToken cancellationToken
) =>
{
    return await createTodoUseCase.ExecuteAsync(request, cancellationToken);
});
```

Use Caseのコンストラクターには、その処理に必要なRepository、Logger、TimeProviderだけを指定します。これにより、不要な依存関係が増えません。

## 4. 冪等性キーとの関係

POSTでは、同じ作成処理を冪等性キーなしでも、冪等性ストア経由でも実行します。どちらの場合も実際のTodo作成処理は`CreateTodoUseCase.ExecuteAsync`です。

```csharp
() => createTodoUseCase.ExecuteAsync(request, cancellationToken)
```

冪等性ストアは「同じキーの処理結果を共有する」責務、Use Caseは「Todoを作成して保存する」責務です。役割が分かれているため、処理の流れを追いやすくなります。

## 学習ポイント

- EndpointはHTTP、Use Caseはアプリケーション操作を担当する
- QueryとCommandを分けると責務を整理しやすい
- Use Caseへ必要な依存だけをDIする
- 複数の入口から同じUse Caseを再利用できる
- Use Case分離はCQRSそのものではなく、CQRSへ進む前の整理にもなる
