# メトリクスを理解する

ログは個別の出来事を調査するために使い、メトリクスは数値を集計して状態を把握するために使います。

## 1. ログとの違い

```text
ログ      -> 何が起きたかを調査
メトリクス -> どれくらい起きているかを集計
```

このAPIでは、HTTPリクエストの件数と処理時間を記録します。

## 2. MeterとInstrument

```csharp
var meter = new Meter("SampleDotnet.TodoApi", "1.0");
var counter = meter.CreateCounter<long>("todo_api.http.requests");
var histogram = meter.CreateHistogram<double>("todo_api.http.request.duration");
```

`Meter`はメトリクスの入れ物です。
`Counter`は増加する値、`Histogram`は処理時間など分布を持つ値に使います。

## 3. タグとカーディナリティ

このAPIでは、メソッドとステータスコードをタグにします。

```text
http.method=GET
http.status_code=200
```

TodoのIDや生のURLをタグにすると値の種類が増えすぎるため、メトリクスのタグには使いません。
この性質をカーディナリティと呼びます。

## 練習問題

- 500エラーの割合をメトリクスからどう計算できるか
- なぜTodo IDをタグにしないのか
- ログとメトリクスを同じ用途にしない理由は何か
