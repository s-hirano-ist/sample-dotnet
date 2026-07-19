# CORSを理解する

CORSは、ブラウザが別のオリジンにあるAPIへアクセスするときの許可ルールです。

## 1. オリジンとは

オリジンは、次の3つの組み合わせです。

```text
スキーム + ホスト + ポート
```

例えば、次の2つはポートが違うため別のオリジンです。

```text
フロントエンド: http://localhost:3000
Todo API:      http://localhost:5191
```

## 2. 同一オリジン制約

ブラウザには、悪意のあるWebサイトから別のサイトのAPIを勝手に呼ばせないための制限があります。

```text
ブラウザ上のJavaScript
  ↓
別オリジンのAPIへリクエスト
  ↓
ブラウザがCORSルールを確認
```

curlやサーバー間通信には、ブラウザの同一オリジン制約はありません。CORSは主にブラウザのための仕組みです。

## 3. Originヘッダー

ブラウザは、別オリジンへリクエストするとき`Origin`ヘッダーを付けます。

```http
Origin: http://localhost:3000
```

APIサーバーは、許可したオリジンであればレスポンスに次のヘッダーを返します。

```http
Access-Control-Allow-Origin: http://localhost:3000
```

ブラウザは、このレスポンスヘッダーを確認して、JavaScriptへレスポンスを公開します。

## 4. プリフライトリクエスト

ブラウザは、POSTやカスタムヘッダーを使うリクエストの前に、`OPTIONS`を送ることがあります。
これをプリフライトリクエストと呼びます。

```http
OPTIONS /todos
Origin: http://localhost:3000
Access-Control-Request-Method: POST
Access-Control-Request-Headers: content-type, x-api-key
```

APIは、許可しているOrigin・メソッド・ヘッダーをレスポンスで返します。
ブラウザはその内容を確認してから、本来のPOSTを送信します。

## 5. CORSポリシーの登録

このAPIでは、`AddCors`でポリシーを登録しています。

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

この設定は次の意味です。

```text
Frontendという名前のポリシーを作る
許可するオリジンを指定する
任意のリクエストヘッダーを許可する
任意のHTTPメソッドを許可する
```

許可するオリジンは`appsettings.json`から読み込んでいます。

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"
    ]
  }
}
```

## 5. CORSポリシーの適用

登録したポリシーは、`UseCors`でHTTPパイプラインへ適用します。

```csharp
app.UseCors("Frontend");
```

登録だけではリクエストへ適用されません。

```text
AddCors  -> ルールを登録
UseCors  -> ルールを実行
```

## 6. プリフライトリクエスト

ブラウザは、実際のリクエストの前に`OPTIONS`リクエストを送ることがあります。これをプリフライトリクエストと呼びます。

```text
OPTIONS /todos
  ↓
このOrigin、メソッド、ヘッダーを使ってよいか確認
  ↓
実際のPOSTやPUTを送る
```

特に次のような場合にプリフライトが発生しやすくなります。

- `POST`や`PUT`でJSONを送る
- `X-API-Key`などのカスタムヘッダーを付ける
- 単純なリクエストではないContent-Typeを使う

## 7. CORSと認証の関係

CORSと認証は別の仕組みです。

```text
CORS       -> そのブラウザOriginからのアクセスを許可するか
認証       -> APIキーなどで利用者を確認するか
認可       -> その利用者に操作を許可するか
```

例えば、CORSでOriginが許可されていても、APIキーがなければ`POST /todos`は`401`になります。

## 8. 本番での注意点

開発中は`http://localhost:3000`を許可していますが、本番では実際のフロントエンドOriginだけを指定します。

```csharp
// 広すぎる設定は避ける
policy.AllowAnyOrigin();
```

認証情報を扱うAPIで、任意のOriginを許可すると意図しないWebサイトからの利用を許してしまう可能性があります。

## 練習問題

次のリクエストについて説明してみてください。

```http
GET /todos HTTP/1.1
Origin: http://localhost:3000
```

確認するポイント:

- `Origin`は何を表すか
- APIがどの設定を使って許可を判断するか
- 許可された場合にレスポンスへ付くヘッダー
- CORSとAPIキー認証の違い
