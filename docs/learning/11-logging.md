# .NETのログを理解する

ログは、アプリケーションが何をしているかを後から確認するための記録です。

## 1. `ILogger<T>`

ASP.NET Coreでは、`ILogger<T>`をDIから受け取ってログを出力します。

```csharp
public class TodoService
{
    private readonly ILogger<TodoService> _logger;

    public TodoService(ILogger<TodoService> logger)
    {
        _logger = logger;
    }
}
```

`ILogger<TodoService>`の`T`は、ログのカテゴリになります。

```text
カテゴリ: TodoService
```

## 2. ログレベル

ログには重要度があります。

```csharp
_logger.LogTrace("詳細な追跡情報");
_logger.LogDebug("デバッグ情報");
_logger.LogInformation("通常の処理が完了");
_logger.LogWarning("想定内だが注意が必要");
_logger.LogError(exception, "処理に失敗");
_logger.LogCritical(exception, "重大な障害");
```

使い分けの目安:

| レベル | 用途 |
| --- | --- |
| `Trace` | 非常に詳細な処理の追跡 |
| `Debug` | 開発時のデバッグ |
| `Information` | 通常の処理記録 |
| `Warning` | エラーではないが注意が必要 |
| `Error` | 処理に失敗した |
| `Critical` | アプリ継続が難しい重大障害 |

## 3. 構造化ログ

現在の`TodoService`では、プレースホルダーを使っています。

```csharp
_logger.LogInformation("Created todo with id {TodoId}", todo.Id);
```

これは単なる文字列連結ではありません。`TodoId`という名前付きの値を持つログです。

```text
メッセージ: Created todo with id {TodoId}
値: TodoId = 1
```

次のような文字列連結は避けます。

```csharp
// 避ける例
_logger.LogInformation("Created todo with id " + todo.Id);
```

構造化ログにすると、ログ基盤で`TodoId = 1`のように検索しやすくなります。

## 4. ログレベルの設定

`appsettings.json`でカテゴリごとのログレベルを設定できます。

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TodoService": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

`Information`以上を出力する設定なら、Information、Warning、Error、Criticalが対象になります。

```text
Trace       出力しない
Debug       出力しない
Information 出力する
Warning     出力する
Error       出力する
Critical    出力する
```

## 5. `BeginScope`とRequest ID

Request ID Middlewareでは、ログスコープへRequest IDを追加しています。

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["RequestId"] = requestId
}))
{
    await _next(context);
}
```

このスコープの中で出力されたログには、同じRequest IDを付けられます。

```text
HTTP GET /todos returned 200
RequestId: 11111111111111111111111111111111
```

レスポンスの`X-Request-Id`とログのRequest IDを一致させることで、1回のリクエストを追跡できます。

## 6. ログへ出してはいけない情報

ログは複数の人やサービスから見られる可能性があります。

出力を避ける情報:

- APIキー
- パスワード
- アクセストークン
- クレジットカード情報
- リクエスト本文全体
- 個人情報の不要な詳細

このAPIでは、Todoのタイトル本文や認証ヘッダーをログへ出さず、IDやステータスなど必要な情報だけを記録しています。

認証失敗ログには`EventId`も付けています。ログ基盤では、文章の検索だけでなくイベント番号や名前で集計できます。

`LoggerMessage`を使うと、構造化ログの定義をソース生成へ任せられます。
ログレベル、EventId、メッセージテンプレートを一か所へまとめられるため、繰り返し出力されるログに向いています。

## 7. HTTPアクセスログ

`RequestLoggingMiddleware`では、処理の前後で時間を計測しています。

```csharp
var stopwatch = Stopwatch.StartNew();

try
{
    await _next(context);
}
finally
{
    stopwatch.Stop();
    _logger.LogInformation(
        "HTTP {HttpMethod} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        stopwatch.ElapsedMilliseconds
    );
}
```

`finally`なので、成功・失敗に関係なくアクセスログを残しやすくなります。

## 練習問題

次のログの違いを説明してみてください。

```csharp
_logger.LogInformation("Todo created: " + todo.Id);
_logger.LogInformation("Todo created with id {TodoId}", todo.Id);
```

確認するポイント:

- 構造化ログになっているのはどちらか
- ログ基盤でID検索しやすいのはどちらか
- APIキーをログへ出してはいけない理由
- Request IDが運用で役立つ場面
