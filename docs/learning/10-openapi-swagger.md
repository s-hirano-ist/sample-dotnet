# OpenAPIとSwagger UIを理解する

OpenAPIは、HTTP APIの仕様を機械可読な形式で表す標準です。

## 1. API仕様とは

APIを利用するには、次の情報が必要です。

- URL
- HTTPメソッド
- パスパラメータ
- クエリパラメータ
- リクエストボディ
- レスポンス形式
- ステータスコード
- 認証方法

例えばTodo作成APIには、次のような仕様があります。

```text
POST /todos
認証: X-API-Keyが必要
入力: { "title": "Learn .NET" }
成功: 201 Created
```

## 2. OpenAPI JSON

このAPIでは、`AddOpenApi`でOpenAPI機能を登録しています。

```csharp
builder.Services.AddOpenApi();
```

`MapOpenApi`で仕様書のURLを追加します。

```csharp
app.MapOpenApi();
```

アプリ起動後、次のURLでJSONを取得できます。

```text
http://localhost:5191/openapi/v1.json
```

OpenAPI JSONには、エンドポイントの情報が含まれます。

```json
{
  "openapi": "3.1.0",
  "paths": {
    "/todos": {
      "get": {
        "operationId": "GetTodos"
      }
    }
  }
}
```

## 3. `WithName`とoperationId

エンドポイントには名前を付けています。

```csharp
app.MapGet("/todos", /* 処理 */)
    .WithName("GetTodos");
```

この名前は、OpenAPIの`operationId`などの識別情報に使われます。

エンドポイントへ名前を付けると、仕様書や生成クライアントで識別しやすくなります。

## 4. Swagger UI

Swagger UIは、OpenAPI JSONをブラウザで操作できる画面にするツールです。

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Todo API v1");
    });
}
```

開発環境で次のURLを開くと、APIの一覧を確認できます。

```text
http://localhost:5191/swagger
```

Swagger UIでは、ブラウザからリクエストを送ってレスポンスも確認できます。

## 5. OpenAPI JSONとSwagger UIの違い

```text
OpenAPI JSON: API仕様そのもの
Swagger UI: OpenAPI JSONを表示・操作する画面
```

Swagger UIは仕様を持っているのではなく、OpenAPI JSONを読み込んで画面を作ります。

```text
OpenAPI JSON
  ↓ 読み込む
Swagger UI
  ↓
ブラウザにAPI一覧を表示
```

## 6. 自動生成のメリット

コードから仕様書を生成すると、実装と仕様のずれを減らしやすくなります。

```text
エンドポイントのコード
  ↓
OpenAPI JSON
  ↓
Swagger UI、クライアント生成、テスト資料
```

OpenAPI JSONは、次の用途にも使えます。

- フロントエンド開発者との契約共有
- HTTPクライアントの自動生成
- API仕様のレビュー
- CIでの仕様差分確認
- 外部サービスとの連携

## 7. 開発環境だけでSwagger UIを有効にする理由

Swagger UIは便利ですが、APIの構造を外部へ公開する画面でもあります。

このAPIでは、開発環境だけでUIを有効にしています。

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(/* ... */);
}
```

本番で公開する場合は、認証、ネットワーク制限、公開範囲を検討します。

## 練習問題

次の2つの役割の違いを説明してみてください。

```text
/openapi/v1.json
/swagger
```

確認するポイント:

- どちらが機械可読な仕様書か
- どちらがブラウザ操作用の画面か
- Swagger UIが何を読み込んでいるか
- 本番公開時に確認すべきセキュリティ上の点
