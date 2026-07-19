# 認証と認可を理解する

認証と認可は似た言葉ですが、役割が異なります。

```text
認証: あなたは誰か
認可: その人は何をしてよいか
```

## 1. 認証とは

認証（Authentication）は、リクエストを送った利用者を確認する処理です。

このAPIでは、`X-API-Key`ヘッダーを使っています。

```http
X-API-Key: dev-only-todo-api-key
```

`ApiKeyAuthenticationHandler`がヘッダーの値を確認し、正しければ認証済みとして扱います。

```text
リクエスト
  ↓
X-API-Keyを読む
  ↓
設定値のAPIキーと比較
  ↓
認証成功または失敗
```

## 2. 認証ハンドラー

認証方法は、`AddAuthentication`と`AddScheme`で登録しています。

```csharp
builder.Services
    .AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        "ApiKey",
        _ => { }
    );
```

分解すると次の意味です。

```text
認証方式の名前: ApiKey
認証処理のクラス: ApiKeyAuthenticationHandler
```

`AuthenticationHandler`は、HTTPリクエストから認証情報を読み取り、認証結果をASP.NET Coreへ返すクラスです。

## 3. ClaimsPrincipal

認証に成功すると、ASP.NET Coreは利用者を表す`ClaimsPrincipal`を作成します。

```text
認証成功
  ↓
HttpContext.Userに利用者情報を設定
  ↓
後続の認可処理がUserを確認
```

`HttpContext.User.Identity.IsAuthenticated`で認証済みか確認できます。

認証済みの利用者名や権限などは、Claimとして保持できます。

## 4. 認可とは

認可（Authorization）は、認証済みの利用者が特定の操作をしてよいか判断する処理です。

```csharp
app.MapPost("/todos", /* 処理 */)
    .RequireAuthorization();
```

`RequireAuthorization`を付けたエンドポイントは、認証に成功していないと実行されません。

現在のAPIでは次の操作を保護しています。

```text
POST /todos    認証が必要
PUT /todos/{id} 認証が必要
DELETE /todos/{id} 認証が必要
GET /todos     公開
```

## 5. 401と403

認証・認可では、主に次のステータスコードを使います。

### 401 Unauthorized

認証情報がない、または正しくない場合です。

```text
APIキーがない
APIキーが一致しない
```

### 403 Forbidden

認証は成功したものの、その操作を実行する権限がない場合です。

```text
利用者は確認できた
しかし管理者専用操作だった
```

このAPIの現在のAPIキー認証では、主に401を確認します。将来、ユーザーやロールを追加すると403も重要になります。

このAPIでは、認証に失敗した場合も`application/problem+json`形式で次のようなレスポンスを返します。

```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Authentication is required.",
  "status": 401
}
```

送信されたAPIキーや、正しいキーとの差分はレスポンスへ含めません。

## 6. Middlewareの順番

認証・認可は、エンドポイントより前に設定します。

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

順番は次のようになります。

```text
リクエスト
  ↓
UseAuthentication
  ↓
UseAuthorization
  ↓
エンドポイント
```

認証より先に認可を実行すると、認可が利用する`HttpContext.User`がまだ設定されていません。

## 7. APIキーの注意点

APIキー認証は学習や内部APIには使えますが、利用者ごとの管理や細かい権限制御には向きません。

本番で検討する機能:

- HTTPSで通信を暗号化する
- APIキーをUser Secretsやシークレット管理サービスで管理する
- APIキーをログへ出力しない
- キーのローテーションと失効を行う
- 利用者単位の認証にはJWTやOIDCを検討する
- 権限単位の認可ポリシーを設計する

## 練習問題

次のエンドポイントへAPIキーなしでアクセスした場合の流れを説明してみてください。

```csharp
app.MapPost("/todos", (CreateTodoRequest request) =>
{
    return Results.Created();
})
.RequireAuthorization();
```

確認するポイント:

- どの処理がAPIキーを確認するか
- 認証に失敗した場合、エンドポイントが実行されるか
- 返るHTTPステータスコード
- 認証と認可のどちらが関係しているか
