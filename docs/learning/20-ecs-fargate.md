# ECS on Fargateの構成を理解する

この資料では、公開したコンテナイメージをECS on Fargateで動かすときの構成を整理します。

## 1. 全体の流れ

```text
GitHub Actions
  ↓ コンテナイメージをGHCRへpush
コンテナレジストリ
  ↓ イメージをpull
ECS Task
  ↓
ECS Serviceが複数Taskを管理
  ↓
Application Load Balancer
  ↓
クライアント
```

Fargateでは、サーバーインスタンスを自分で管理せずにTaskを実行します。

## 2. Task Definition

Task Definitionは、コンテナをどう起動するかを定義する設計書です。

主な設定:

- コンテナイメージ
- CPUとメモリ
- コンテナポート
- 環境変数
- Secrets Managerなどの秘密情報
- ログ出力先
- ヘルスチェック
- IAM実行ロール

このプロジェクトでは、次のCompose設定がTask Definitionの考え方に近いです。

```yaml
environment:
  RateLimit__Store: Redis
ports:
  - "8080:8080"
healthcheck:
  test: ["CMD", "curl", "--fail", "http://localhost:8080/health"]
```

## 3. ECS Service

ECS Serviceは、指定した数のTaskを維持します。

```text
desiredCount = 3
  ↓
Task A、Task B、Task Cを維持
```

Taskが異常終了した場合、Service Schedulerが新しいTaskを起動して置き換えます。

## 4. Application Load Balancer

ALBは、外部からのHTTP/HTTPSリクエストをTaskへ転送します。

```text
Client
  ↓ HTTPS
ALB
  ↓ HTTP:8080
ECS Task
```

FargateのTaskはそれぞれ独立した実行単位なので、ALBは複数Taskへリクエストを振り分けます。

## 5. ヘルスチェック

ALBのTarget Groupは、Taskの`/health`へ定期的にアクセスします。

```text
ALB -> GET /health
```

正常なら転送対象として扱い、失敗が続けば対象から外します。

ECSにはコンテナヘルスチェックもあります。コンテナ内のチェックとALBのTarget Groupチェックは別の仕組みです。

```text
コンテナHealthcheck -> コンテナ内部の状態
ALB Healthcheck      -> 外部からHTTPで到達できるか
```

## 6. ポート

このAPIはコンテナ内で8080ポートを待ち受けます。

```dockerfile
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
```

ECSのTask DefinitionとALB Target Groupでも、コンテナ名とポート8080を対応させます。

## 7. 設定と秘密情報

本番のAPIキーやDB接続文字列をDockerfileやGitへ書き込んではいけません。

```text
Task Definitionの環境変数
  -> 一般設定

Secrets Manager / SSM Parameter Store
  -> 秘密情報
```

このリポジトリのComposeでは、学習用に環境変数からAPIキーを渡しています。

```bash
TODO_API_KEY="your-local-api-key" docker compose up --build
```

## 8. 複数Taskと状態共有

複数Taskでは、Taskごとにメモリが異なります。

```text
Task Aのメモリ != Task Bのメモリ
```

Todoデータ、ログ、セッション、分散レート制限カウンターをTaskのメモリだけに保存してはいけません。

このAPIでは、TodoはDB、分散レート制限はRedisへ保存する構成を想定しています。

## 9. ECSでのレート制限

Taskが複数になると、MemoryカウンターはTaskごとに分かれます。

```text
Task A -> MemoryカウンターA
Task B -> MemoryカウンターB
Task C -> MemoryカウンターC
```

全Taskで同じ制限を適用したい場合、Redisなどの共有ストアを使います。

## 参考資料

- [ECSサービスのロードバランシング](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-load-balancing.html)
- [ECSコンテナヘルスチェック](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/healthcheck.html)
- [ECSサービス](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/ecs_services.html)
