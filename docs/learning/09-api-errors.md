# APIのエラー処理を理解する

APIでは、処理が成功したときだけでなく、失敗したときのレスポンスも仕様として設計します。

## 1. HTTPステータスコード

ステータスコードは、リクエストの結果を大まかに伝えます。

| コード | 意味 | Todo APIの例 |
| --- | --- | --- |
| `200` | 成功 | Todo取得、更新 |
| `201` | リソース作成 | Todo作成 |
| `204` | 成功、本文なし | Todo削除 |
| `400` | リクエストが不正 | 空タイトル、無効なページ番号 |
| `401` | 認証が必要または失敗 | APIキーなし |
| `404` | リソースが存在しない | 存在しないTodo ID |
| `429` | リクエスト過多 | レート制限超過 |
| `500` | サーバー内部エラー | 予期しない例外 |

## 2. 想定内のエラー

入力値が不正、または対象のTodoが存在しない場合は、アプリケーションが想定できるエラーです。

```csharp
var validation = TodoValidation.ValidateTitle(request.Title);

if (!validation.IsValid)
{
    return Results.BadRequest(validation.Error);
}
```

この場合は、アプリケーションが自分で`400 Bad Request`を返します。

## 3. ApiError

このAPIでは、入力エラーを`code`と`message`で表しています。

```csharp
public record ApiError(
    string Code,
    string Message
);
```

レスポンス例:

```json
{
  "code": "title_required",
  "message": "Title is required."
}
```

`code`はプログラム向けです。クライアントは`code`を使って表示や分岐を決められます。

`message`は人間が読める説明です。

## 4. 404 Not Found

対象のTodoがない場合は、入力形式は正しいものの、対象リソースがありません。

```csharp
var todo = await todoService.GetByIdAsync(id, cancellationToken);

return todo is null
    ? Results.NotFound()
    : Results.Ok(todo);
```

`null`を返すサービスと、HTTP 404へ変換するAPI層を分けています。

```text
サービス: Todoがない -> null
API層: null -> 404 Not Found
```

## 5. 予期しない例外

DB接続障害など、通常の入力チェックでは予測できない問題が発生することがあります。

```csharp
try
{
    await _next(context);
}
catch (Exception exception)
{
    _logger.LogError(exception, "Unhandled exception while processing the request.");
    // 500のレスポンスを返す
}
```

`ExceptionHandlingMiddleware`は、予期しない例外を共通して処理します。

## 6. ProblemDetails

予期しない例外のレスポンスは、`application/problem+json`形式です。

```json
{
  "type": "https://httpstatuses.com/500",
  "title": "An unexpected error occurred.",
  "status": 500,
  "instance": "/todos",
  "requestId": "リクエストを識別するID"
}
```

`requestId`を返すことで、利用者はサーバーログの該当リクエストを運用担当者へ伝えられます。

## 7. 詳細情報を返さない理由

例外メッセージやスタックトレースには、次のような情報が含まれる可能性があります。

- DB接続文字列
- ファイルパス
- テーブル名
- 内部クラス名
- 秘密情報の一部

そのため、詳細な例外はログへ出し、クライアントには一般的なメッセージだけを返します。

```text
サーバーログ: 詳細な例外情報
APIレスポンス: 一般的なエラー + requestId
```

## 8. クライアント側の扱い

クライアントは、ステータスコードと`code`を使って処理します。

```text
400 + title_required -> 入力欄を修正して再送
401                -> 認証情報を確認
404                -> 対象が削除された可能性を表示
429                -> Retry-Afterを待って再試行
500                -> requestIdを表示して問い合わせ
```

## 練習問題

次の2つの失敗の違いを説明してみてください。

```text
POST /todos に { "title": "" } を送る
存在しないDBへ接続して GET /todos を送る
```

確認するポイント:

- どちらが想定内のエラーか
- それぞれのステータスコード
- エラー詳細をクライアントへ返してよいか
- `requestId`が役立つ場面
