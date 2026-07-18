# Todo API Hands-on

.NETの学習用に作る、シンプルなTodoアプリのbackend REST APIです。

ASP.NET Core Minimal APIで実装し、TodoはSQLiteに保存します。Entity Framework Coreを使ってC#のコードからデータベースを操作します。

OpenAPI仕様書は、アプリ起動後に `http://localhost:5191/openapi/v1.json` で確認できます。ポート番号は起動時の表示に合わせてください。
Swagger UIは `http://localhost:5191/swagger` で確認できます。Swagger UIから各APIをブラウザ上で試すこともできます。

Todoの作成・更新・削除や、存在しないTodoへの更新・削除を行うと、ターミナルに構造化ログが出力されます。

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
│   ├── Services/
│   │   └── TodoService.cs
│   ├── TodoApi.csproj
│   ├── Validation/
│   │   ├── ApiError.cs
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

## APIを試す

Todoを作成します。

```bash
curl -X POST http://localhost:5191/todos \
  -H "Content-Type: application/json" \
  -d '{"title":"Learn .NET Minimal API"}'
```

Todo一覧を取得します。

```bash
curl http://localhost:5191/todos
```

IDを指定して1件取得します。

```bash
curl http://localhost:5191/todos/1
```

Todoを完了にします。

```bash
curl -X PUT http://localhost:5191/todos/1 \
  -H "Content-Type: application/json" \
  -d '{"isDone":true}'
```

Todoのタイトルを変更します。

```bash
curl -X PUT http://localhost:5191/todos/1 \
  -H "Content-Type: application/json" \
  -d '{"title":"Learn C# and ASP.NET Core"}'
```

Todoを削除します。

```bash
curl -X DELETE http://localhost:5191/todos/1
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
