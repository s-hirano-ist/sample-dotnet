# async、await、Taskを理解する

Todo APIでは、データベース処理に`async`と`await`を使っています。

```csharp
var todos = await _dbContext.Todos.ToListAsync(cancellationToken);
```

まず、これらの役割を分けて考えます。

## 1. 同期処理

普通の同期メソッドは、処理が終わるまで次の行へ進みません。

```csharp
public string GetMessage()
{
    return "Hello";
}

var message = GetMessage();
Console.WriteLine(message);
```

`GetMessage()`が終わってから、`Console.WriteLine`が実行されます。

## 2. 非同期処理

データベースや外部APIへのアクセスは、結果を待つ時間が発生します。

```csharp
public async Task<string> GetMessageAsync()
{
    await Task.Delay(1000);
    return "Hello";
}
```

`Task.Delay(1000)`は1秒待つ処理です。`async`メソッドは、結果がすぐに返らないことを表します。

戻り値の`Task<string>`は、次のような意味です。

```text
今すぐstringを返すのではなく、将来stringが得られる処理
```

## 3. `await`の役割

`await`は、非同期処理の結果を待ちます。

```csharp
var message = await GetMessageAsync();
Console.WriteLine(message);
```

このメソッド内では結果を待ちますが、待っている間、スレッドをブロックし続ける必要がありません。

そのため、HTTPリクエストが多いサーバーでも、待ち時間にスレッドを別の処理へ使いやすくなります。

## 4. `async`の役割

メソッド内で`await`を使う場合、通常はメソッドに`async`を付けます。

```csharp
public async Task<TodoItem?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken
)
{
    return await _dbContext.Todos.FirstOrDefaultAsync(
        todo => todo.Id == id,
        cancellationToken
    );
}
```

このメソッドは次の要素を持っています。

```text
async             -> awaitを使うメソッド
Task<TodoItem?>   -> 将来TodoItemまたはnullが得られる
await             -> DB処理の結果を待つ
Async             -> 非同期版のメソッドだと分かる名前
```

## 5. なぜDB処理を非同期にするのか

DBアクセス中は、アプリケーションのCPUが計算しているのではなく、DBの応答を待っています。

```text
APIサーバー -> SQLを送る -> DBの応答を待つ -> 結果を受け取る
```

同期処理で待つと、そのHTTPリクエストを処理しているスレッドが待機し続けます。

非同期処理を使うと、待ち時間にスレッドを他のリクエストへ使いやすくなります。

```csharp
await _dbContext.Todos.ToListAsync(cancellationToken);
```

`ToListAsync`は、EF Coreが提供する非同期のDB読み取りメソッドです。

## 6. `Task`を自分で作るものではない

`Task`は、非同期処理の結果を表す型です。多くの場合、EF CoreやHTTPクライアントが作った`Task`を`await`します。

```csharp
var task = _dbContext.Todos.ToListAsync(cancellationToken);
var todos = await task;
```

通常は、次のように1行で書きます。

```csharp
var todos = await _dbContext.Todos.ToListAsync(cancellationToken);
```

`Task`を返すメソッドを、次のように同期的に待つのは避けます。

```csharp
// 避ける例
var todos = _dbContext.Todos.ToListAsync().Result;
```

`.Result`や`.Wait()`はスレッドをブロックし、状況によってはデッドロックやスレッド不足の原因になります。

## 7. `CancellationToken`との関係

非同期処理へ`CancellationToken`を渡すと、処理を途中でキャンセルできます。

```csharp
await _dbContext.Todos.ToListAsync(cancellationToken);
```

流れは次の通りです。

```text
リクエストが切断される
  ↓
CancellationTokenがキャンセル状態になる
  ↓
EF CoreがDB処理のキャンセルを試みる
  ↓
不要な処理を早く終了する
```

すべての非同期メソッドがキャンセルに対応するとは限らないため、対応するメソッドへトークンを渡すことが重要です。

## 練習問題

次のコードを説明してみてください。

```csharp
public async Task<TodoItem?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken
)
{
    return await _dbContext.Todos.FirstOrDefaultAsync(
        todo => todo.Id == id,
        cancellationToken
    );
}
```

確認するポイント:

- `Task<TodoItem?>`は何を表すか
- `async`は何を示すか
- `await`は何を待つか
- `CancellationToken`は何のためにあるか
