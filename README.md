# Todo API Hands-on

.NETの学習用に作る、シンプルなTodoアプリのbackend REST APIです。

ASP.NET Core Minimal APIで実装し、TodoはSQLiteに保存します。Entity Framework Coreを使ってC#のコードからデータベースを操作します。

OpenAPI仕様書は、アプリ起動後に `http://localhost:5191/openapi/v1.json` で確認できます。ポート番号は起動時の表示に合わせてください。
Swagger UIは `http://localhost:5191/swagger` で確認できます。Swagger UIから各APIをブラウザ上で試すこともできます。

Todoの作成・更新・削除や、存在しないTodoへの更新・削除を行うと、ターミナルに構造化ログが出力されます。

各レスポンスには`X-Request-Id`が付与されます。このIDはログにも含まれるため、レスポンスとサーバーログを紐付けられます。

HTTPメソッド、パス、ステータスコード、処理時間も構造化ログへ記録されます。リクエスト本文や認証ヘッダーはログに出しません。

アプリとデータベースの状態は `http://localhost:5191/health` で確認できます。

Todo APIにはクライアント単位のレート制限があり、設定ファイルの`RateLimit`で保存先、許可数、時間枠を管理します。初期設定はインメモリで、10秒間に最大10リクエストまで許可します。制限を超えると`429 Too Many Requests`が返ります。

Todo一覧はページング、完了状態フィルター、タイトル検索、ソートに対応しています。`GET /todos`はデフォルトで1ページ目を20件返し、`page`と`pageSize`で取得範囲を指定できます。`pageSize`は最大100件です。`isDone=true`または`isDone=false`を指定すると、完了状態で絞り込めます。`search`を指定すると、タイトルにその文字列を含むTodoだけを取得します。`sortBy`は`id`、`title`、`createdAt`を指定でき、`sortOrder`は`asc`または`desc`を指定できます。

APIのDB処理にはリクエストの`CancellationToken`を渡しています。クライアントが通信を中断した場合、EF Coreの検索や保存もキャンセルできる構成です。

## 必要なもの

- .NET SDK
- ターミナル
- curlなどのHTTPリクエストを送れるツール

インストール済みの.NET SDKは次で確認できます。

```bash
dotnet --version
```

## プロジェクト構成

```text
.
├── README.md
├── SampleDotnet.slnx
├── dotnet-tools.json
├── TodoApi/
│   ├── Data/
│   │   ├── TodoDbContext.cs
│   │   └── TodoDbContextFactory.cs
│   ├── Migrations/
│   │   ├── 20260711024515_InitialCreate.cs
│   │   ├── 20260711024515_InitialCreate.Designer.cs
│   │   └── TodoDbContextModelSnapshot.cs
│   ├── Models/
│   │   └── TodoItem.cs
│   ├── Program.cs
│   ├── Requests/
│   │   ├── CreateTodoRequest.cs
│   │   └── UpdateTodoRequest.cs
│   ├── Responses/
│   │   └── TodoListResponse.cs
│   ├── Services/
│   │   └── TodoService.cs
│   ├── TodoApi.csproj
│   ├── Validation/
│   │   ├── ApiError.cs
│   │   ├── PaginationValidation.cs
│   │   ├── TodoValidation.cs
│   │   └── ValidationResult.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── TodoApi.Tests/
│   ├── TodoApiTests.cs
│   └── TodoApi.Tests.csproj
└── mise.toml
```

APIの入口は `TodoApi/Program.cs`、DB接続は `TodoApi/Data/`、DB変更履歴は `TodoApi/Migrations/`、Todoのデータ構造は `TodoApi/Models/`、リクエストの形は `TodoApi/Requests/`、Todo操作の処理は `TodoApi/Services/`、入力チェックとエラー形式は `TodoApi/Validation/` にあります。

テストは `TodoApi.Tests/TodoApiTests.cs` にあります。

## 起動する

リポジトリのルートで次を実行します。

```bash
dotnet run --project TodoApi
```

初回起動時に、SQLiteの `todo.db` が作成されます。`todo.db` はローカル開発用のデータベースなので、git管理対象には入れていません。

起動すると、次のようなURLが表示されます。

```text
Now listening on: http://localhost:5191
```

ポート番号は環境によって変わることがあります。以降の例では `http://localhost:5191` と書きますが、自分のターミナルに表示されたURLに読み替えてください。

起動確認:

```bash
curl http://localhost:5191
```

次のように返れば起動できています。

```text
Todo API is running.
```

ヘルスチェックを確認:

```bash
curl -i http://localhost:5191/health
```

アプリとSQLiteへ接続できていれば`200 OK`が返ります。RedisモードではRedisへの接続も確認し、Redisが利用できない場合は`503 Service Unavailable`を返します。監視システムやロードバランサーは、このような専用URLを定期的に確認します。

## レート制限を確認する

レート制限は、短時間に大量のリクエストが送られることを防ぐ仕組みです。同じクライアントからTodo APIへ11回以上連続でアクセスすると、制限に達したリクエストで`429`が返ります。`Retry-After`ヘッダーには再試行までの秒数が入ります。

```bash
for i in $(seq 1 11); do
  curl -s -o /dev/null -w "request=$i status=%{http_code}\n" http://localhost:5191/todos
done
```

## Redisによる分散レート制限

ECS/Fargateなどで複数コンテナを起動する場合、インメモリのカウンターはコンテナごとに分かれます。Redisを共有カウンターにすると、複数コンテナで同じ制限状態を使えます。

ローカルRedisを起動する例:

```bash
docker run --name sample-dotnet-redis -p 6379:6379 -d redis:7-alpine
```

Redisモードを有効にする設定:

```bash
dotnet user-secrets set "RateLimit:Store" "Redis" --project TodoApi
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project TodoApi
```

Redisモードでは、Luaスクリプトでカウンター増加と有効期限設定をRedis内で一度に行います。Redisに接続できない場合は、レート制限を無効にせず`503 Service Unavailable`を返します。

OpenAPI仕様書を確認:

```bash
curl http://localhost:5191/openapi/v1.json
```

OpenAPIは、APIのURL、HTTPメソッド、リクエスト、レスポンスなどをJSONで表す標準です。

Swagger UIを確認:

ブラウザで次のURLを開きます。

```text
http://localhost:5191/swagger
```

Swagger UIはOpenAPI仕様書を読み込み、APIの一覧表示やリクエスト送信を行える画面です。今回は開発環境でのみ有効にしています。

## ログを確認する

アプリを起動したターミナルで、Swagger UIやcurlからTodoを操作します。次のようなログが表示されます。

```text
info: TodoService[0]
      Created todo with id 1
warn: TodoService[0]
      Todo with id 999 was not found for delete
```

`ILogger<TodoService>`はDIコンテナから自動で渡されます。`{TodoId}`のようなプレースホルダーを使うと、ログメッセージと値を分けて扱える構造化ログになります。タイトル本文はログに出さず、必要最小限の情報だけを記録しています。

アクセスログの例:

```text
HTTP GET /todos returned 200 in 12 ms
HTTP POST /todos returned 401 in 1 ms
HTTP GET /todos returned 429 in 0 ms
```

## 予期しない例外のレスポンス

予期しない例外が発生した場合、APIは詳細な例外メッセージをクライアントへ返さず、HTTP 500と`application/problem+json`形式のレスポンスを返します。

```json
{
  "type": "https://httpstatuses.com/500",
  "title": "An unexpected error occurred.",
  "status": 500,
  "instance": "/todos",
  "requestId": "リクエストを識別するID"
}
```

詳細な例外情報はサーバーログへ出力し、レスポンスの`requestId`を使って該当ログを探します。

## CORSを確認する

CORSは、ブラウザ上のフロントエンドから別のオリジンにあるAPIを呼び出すときの許可ルールです。現在は`appsettings.json`にある`http://localhost:3000`だけを許可しています。

許可されたOriginを付けてリクエストすると、CORSヘッダーが返ります。

```bash
curl -i http://localhost:5191/ \
  -H "Origin: http://localhost:3000"
```

レスポンスに次のヘッダーがあれば、CORS設定が適用されています。

```text
Access-Control-Allow-Origin: http://localhost:3000
```

## Request IDを確認する

クライアントが`X-Request-Id`を送ると、APIは形式を確認して同じリクエストIDを返します。IDを送らない場合はAPIが新しいIDを生成します。

```bash
curl -i http://localhost:5191/ \
  -H "X-Request-Id: 11111111-1111-1111-1111-111111111111"
```

## 認証・認可を確認する

Todoの作成・更新・削除には、`X-API-Key`ヘッダーが必要です。一覧取得やSwagger UIなどのGET操作は公開しています。

APIキーは`appsettings.json`には保存しません。ローカル開発では.NET User Secretsへ登録します。

初回だけ、TodoApiプロジェクトで次を実行します。

```bash
dotnet user-secrets init --project TodoApi
dotnet user-secrets set "Authentication:ApiKey" "dev-only-todo-api-key" --project TodoApi
```

User Secretsはプロジェクト外のローカル領域に保存されるため、Gitへコミットされません。User Secretsの値は`appsettings.json`より優先して読み込まれます。

認証付きでTodoを作成します。

```bash
curl -X POST http://localhost:5191/todos \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-only-todo-api-key" \
  -d '{"title":"Learn authentication"}'
```

APIキーなしで作成すると`401 Unauthorized`になります。

```bash
curl -i -X POST http://localhost:5191/todos \
  -H "Content-Type: application/json" \
  -d '{"title":"This should fail"}'
```

今回のAPIキーは学習用です。本番環境では、キーをソースコードや通常の設定ファイルに直接書かず、環境変数やシークレット管理サービスを利用します。

## APIを試す

Todoを作成します。

```bash
curl -X POST http://localhost:5191/todos \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-only-todo-api-key" \
  -d '{"title":"Learn .NET Minimal API"}'
```

Todo一覧を取得します。

```bash
curl http://localhost:5191/todos
```

2ページ目を2件ずつ取得する例:

```bash
curl "http://localhost:5191/todos?page=2&pageSize=2"
```

レスポンスには`items`のほか、現在のページ、1ページの件数、全件数、全ページ数が含まれます。

未完了のTodoだけを取得する例:

```bash
curl "http://localhost:5191/todos?isDone=false"
```

タイトルに`learn`を含むTodoを取得する例:

```bash
curl "http://localhost:5191/todos?search=learn"
```

タイトルの降順で取得する例:

```bash
curl "http://localhost:5191/todos?sortBy=title&sortOrder=desc"
```

IDを指定して1件取得します。

```bash
curl http://localhost:5191/todos/1
```

Todoを完了にします。

```bash
curl -X PUT http://localhost:5191/todos/1 \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-only-todo-api-key" \
  -d '{"isDone":true}'
```

Todoのタイトルを変更します。

```bash
curl -X PUT http://localhost:5191/todos/1 \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-only-todo-api-key" \
  -d '{"title":"Learn C# and ASP.NET Core"}'
```

Todoを削除します。

```bash
curl -X DELETE http://localhost:5191/todos/1 \
  -H "X-API-Key: dev-only-todo-api-key"
```

## ビルドする

ビルドは、コードがコンパイルできるか確認するためのコマンドです。

```bash
dotnet build TodoApi/TodoApi.csproj
```

ソリューション全体をビルドする場合は次です。

```bash
dotnet build
```

成功すると、最後に次のような表示が出ます。

```text
Build succeeded.
```

エラーが出た場合は、`Program.cs` の文法ミスや型の不一致がないか確認します。

## テストする

このプロジェクトでは、xUnitを使ってAPIの自動テストを書いています。リポジトリのルートで次を実行します。

```bash
dotnet test
```

テストは `TodoApi.Tests/TodoApiTests.cs` にあります。

現在のテストでは、テスト用にAPIをメモリ上で起動し、`HttpClient` で実際のHTTPリクエストに近い形で確認しています。これにより、curlで手動確認しなくても、APIの基本動作をまとめて検証できます。

テストでは本物の `todo.db` は使わず、テストごとにインメモリSQLiteを作っています。そのため、テスト実行でローカル開発用DBの中身は変わりません。

今ある主なテスト:

- `GET /` が起動確認メッセージを返す
- `GET /todos` が空配列を返す
- `POST /todos` でTodoを作成できる
- 空タイトルの `POST /todos` が `400 Bad Request` を返す
- 長すぎるタイトルの `POST /todos` が `400 Bad Request` を返す
- 存在しないIDの `GET /todos/{id}` が `404 Not Found` を返す
- `PUT /todos/{id}` でTodoを更新できる
- 存在しないIDの `PUT /todos/{id}` が `404 Not Found` を返す
- 空タイトルの `PUT /todos/{id}` が `400 Bad Request` を返す
- `DELETE /todos/{id}` でTodoを削除できる
- 存在しないIDの `DELETE /todos/{id}` が `404 Not Found` を返す
- `GET /openapi/v1.json` でOpenAPI仕様書を取得できる
- 開発環境の`/swagger`でSwagger UIを表示できる

## マイグレーション

このプロジェクトでは、EF Coreのマイグレーションでデータベースのテーブル定義を管理しています。

ローカルツールを復元する場合:

```bash
dotnet tool restore
```

新しいマイグレーションを作成する場合:

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project TodoApi/TodoApi.csproj \
  --startup-project TodoApi/TodoApi.csproj \
  --output-dir Migrations
```

今回のアプリは起動時に `Database.Migrate()` を実行するため、未適用のマイグレーションは起動時にSQLiteへ反映されます。

## よく使うコマンドまとめ

```bash
# アプリを起動する
dotnet run --project TodoApi

# APIプロジェクトだけビルドする
dotnet build TodoApi/TodoApi.csproj

# ソリューション全体をビルドする
dotnet build

# 自動テストを実行する
dotnet test

# EF Coreローカルツールを復元する
dotnet tool restore
```
