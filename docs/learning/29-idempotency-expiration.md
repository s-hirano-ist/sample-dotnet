# 冪等性キーの有効期限を理解する

冪等性キーを無期限に保存すると、キーの数だけメモリ使用量が増え続けます。そこで、キーごとに保持期限を設定します。

このAPIでは`Idempotency:EntryLifetimeSeconds`で保持秒数を設定し、期限を過ぎたキーは次のリクエスト処理時に削除します。

```json
{
  "Idempotency": {
    "EntryLifetimeSeconds": 300
  }
}
```

## Optionsと起動時検証

設定を`IdempotencyOptions`へバインドし、`IdempotencyOptionsValidator`で1秒以上24時間以内か検証します。`ValidateOnStart`を使うため、間違った設定をリクエスト受信後ではなく起動時に検知できます。

## TimeProvider

`TimeProvider`をDIから受け取ることで、実際の時刻を使うコードとテスト用の時刻を差し替えるコードを同じにできます。テストでは時刻を進め、期限切れを待たずに検証しています。

## 本番での注意点

既定値は1プロセス内のインメモリ保存です。`Idempotency:Store`を`Redis`にすると、複数コンテナでキーを共有できます。また、分散保存ではTTL、保存するレスポンスのサイズ、同時実行時の原子性も設計します。

同一プロセス内の同時実行では、`Lazy<Task<TodoItem>>`によって作成処理を1回だけ実行し、後続リクエストは同じTaskの結果を待ちます。この動作は同時実行テストで確認しています。
