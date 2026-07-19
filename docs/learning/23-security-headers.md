# セキュリティヘッダーを理解する

HTTPレスポンスヘッダーには、ブラウザの動作を制限して攻撃の影響を減らすものがあります。

## 1. Middlewareで追加する

```csharp
context.Response.Headers["X-Content-Type-Options"] = "nosniff";
context.Response.Headers["X-Frame-Options"] = "DENY";
context.Response.Headers["Referrer-Policy"] = "no-referrer";
```

Middlewareにまとめると、個々のエンドポイントへ同じ処理を書く必要がありません。

## 2. 今回追加したヘッダー

- `X-Content-Type-Options: nosniff`
  - ブラウザがContent-Typeを推測して別形式として扱う動作を抑えます。
- `X-Frame-Options: DENY`
  - APIをiframeへ埋め込むことを禁止します。
- `Referrer-Policy: no-referrer`
  - RefererヘッダーでURL情報を送らないようにします。
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
  - HTTPS接続時、ブラウザへ以後もHTTPSを使うよう伝えます。

## 3. `HasStarted`の確認

レスポンス本文やヘッダーの送信が始まった後は、ヘッダーを変更できません。
そのため、`_next`を呼ぶ前に`Response.HasStarted`を確認してヘッダーを追加します。

HSTSはHTTPSリクエストにだけ付与します。HTTPのローカル開発へ付与すると、ブラウザが以後のローカルアクセスまでHTTPSへ変更する可能性があります。

## 練習問題

- なぜ全エンドポイントへ同じヘッダーを付ける処理をMiddlewareにまとめるのか
- `Response.HasStarted`が`true`のとき、ヘッダーを変更するとどうなるか
- Swagger UIを同じAPIで提供する場合、`X-Frame-Options: DENY`にどのような影響があるか
