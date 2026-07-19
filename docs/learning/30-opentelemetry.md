# OpenTelemetryで観測可能性を高める

OpenTelemetryは、ログ・メトリクス・トレースを共通形式で収集するための標準です。

このAPIでは、設定を有効にすると次を収集します。

- ASP.NET CoreのHTTPトレース
- HttpClientの外部HTTP呼び出しトレース
- HTTPリクエストの標準メトリクス
- .NETランタイムのメトリクス
- 既存の`ApiMetrics`が作るTodo API固有メトリクス

## OTLP

OTLPは、OpenTelemetry Collectorや監視サービスへテレメトリを送るための標準プロトコルです。

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "OtlpEndpoint": "http://otel-collector:4317"
  }
}
```

Collector未接続のローカル環境では`Enabled`を`false`にします。アプリの起動時にCollectorへの接続を必須にしないことで、テストや単独開発を妨げません。

## 学習ポイント

- ログ、メトリクス、トレースはどのように使い分けるか
- `Activity`が分散トレースの1区間を表す理由は何か
- なぜアプリから監視サービスへ直接送らずCollectorを挟む構成があるのか
- メトリクスへユーザーIDやURL全文をタグとして追加してはいけない理由は何か
