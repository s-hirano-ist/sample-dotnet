# ヘルスチェックを理解する

ヘルスチェックは、アプリケーションや依存サービスが正常に動作できるか確認する仕組みです。

## 1. `/health`の役割

このAPIでは、次のURLを公開しています。

```text
GET /health
```

ロードバランサーやコンテナオーケストレーターは、このURLを定期的に呼び出して状態を確認できます。

```text
監視システム
  ↓ GET /health
Todo API
  ↓
DBやRedisを確認
  ↓
200または503を返す
```

## 2. ヘルスチェックの登録

`AddHealthChecks`でヘルスチェック機能を登録します。

```csharp
var healthChecks = builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<TodoDbContext>();
```

このコードでは、Todo APIが使うSQLiteデータベースを確認対象にしています。

Redisモードの場合は、Redisのチェックも追加します。

```csharp
if (useRedisRateLimit)
{
    healthChecks.AddCheck<RedisHealthCheck>("redis");
}
```

## 3. エンドポイントへ公開する

登録したチェックは、`MapHealthChecks`でHTTPエンドポイントへ公開します。

```csharp
app.MapHealthChecks("/health")
    .WithName("GetHealth");
```

`AddHealthChecks`はチェックを登録し、`MapHealthChecks`はチェックを実行するURLを追加します。

```text
AddHealthChecks    -> チェックを登録
MapHealthChecks    -> HTTPで実行できるようにする
```

## 4. HTTPステータスコード

すべての必須チェックが成功すると、通常は`200 OK`を返します。

```http
HTTP/1.1 200 OK
```

DBなどのチェックが失敗すると、通常は`503 Service Unavailable`を返します。

```http
HTTP/1.1 503 Service Unavailable
```

`503`は「アプリは存在するが、現在サービスを提供できない」という意味です。

このAPIのヘルスチェックは、次のような最小限のJSONを返します。

```json
{"status":"Healthy"}
```

DB接続文字列や例外の詳細はレスポンスへ含めません。監視に必要な情報と、外部へ公開してよい情報を分けます。

## 5. LivenessとReadiness

運用では、ヘルスチェックを2つの目的に分けて考えます。

### Liveness

アプリケーションのプロセス自体が生きているかを確認します。

```text
プロセスが動いているか
```

Livenessが失敗した場合、コンテナを再起動する判断に使われます。

### Readiness

リクエストを受け付けられる状態かを確認します。

```text
DBやRedisなど、必要な依存先へ接続できるか
```

Readinessが失敗した場合、ロードバランサーから一時的に外す判断に使われます。

現在の`/health`はDBとRedisを含む依存チェックなので、Readinessに近い役割です。

このAPIでは、用途を明確にするため次のURLも公開しています。

```text
GET /live   -> プロセスが動いているかだけを確認
GET /ready  -> DBやRedisなどの依存サービスを確認
GET /health -> これまでの互換用エンドポイント
```

`/live`ではチェックを実行しないように`Predicate = _ => false`を指定します。
`/ready`は通常の`MapHealthChecks`なので、登録済みのDB・Redisチェックが実行されます。

## 6. 外部サービスのチェック

Redisのヘルスチェックは`IHealthCheck`を実装しています。

```csharp
public class RedisHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _connectionMultiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (RedisException exception)
        {
            return HealthCheckResult.Unhealthy(
                "Redis is unavailable.",
                exception
            );
        }
    }
}
```

チェック自体も非同期で実行し、キャンセルに対応できます。

## 7. ヘルスチェックの注意点

ヘルスチェックは、通常のAPI機能とは目的が異なります。

- 軽量にする
- 認証なしで監視システムから呼べるようにすることが多い
- ただし詳細な内部情報は外部へ返さない
- 依存サービスを確認しすぎて、チェック自体が負荷にならないようにする
- LivenessとReadinessの目的を混ぜない

## 練習問題

次の状態で、`/health`がどうなるか考えてみてください。

```text
アプリは起動している
SQLiteへ接続できない
```

確認するポイント:

- アプリのプロセスは生きているか
- Todo APIは正常にリクエストを処理できるか
- HTTPステータスコードは`200`か`503`か
- LivenessとReadinessのどちらに近い状態か
