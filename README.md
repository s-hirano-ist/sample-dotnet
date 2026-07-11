# Todo API Hands-on

.NETの学習用に作る、シンプルなTodoアプリのbackend REST APIです。

まずは最小構成として、ASP.NET Core Minimal APIで実装しています。データベースはまだ使わず、Todoはアプリのメモリ上に保存します。そのため、アプリを再起動すると登録したTodoは消えます。

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
├── TodoApi/
│   ├── Models/
│   │   └── TodoItem.cs
│   ├── Program.cs
│   ├── Requests/
│   │   ├── CreateTodoRequest.cs
│   │   └── UpdateTodoRequest.cs
│   ├── Services/
│   │   └── InMemoryTodoService.cs
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

APIの入口は `TodoApi/Program.cs`、Todoのデータ構造は `TodoApi/Models/`、リクエストの形は `TodoApi/Requests/`、Todo操作の処理は `TodoApi/Services/`、入力チェックとエラー形式は `TodoApi/Validation/` にあります。

テストは `TodoApi.Tests/TodoApiTests.cs` にあります。

## 起動する

リポジトリのルートで次を実行します。

```bash
dotnet run --project TodoApi
```

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
```
