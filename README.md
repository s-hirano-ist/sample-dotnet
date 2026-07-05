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
├── TodoApi/
│   ├── Program.cs
│   ├── TodoApi.csproj
│   ├── appsettings.json
│   └── appsettings.Development.json
└── mise.toml
```

主に触るファイルは `TodoApi/Program.cs` です。

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

成功すると、最後に次のような表示が出ます。

```text
Build succeeded.
```

エラーが出た場合は、`Program.cs` の文法ミスや型の不一致がないか確認します。

## テストする

現時点ではテストプロジェクトをまだ作っていないため、次のコマンドを実行しても実行対象のテストはありません。

```bash
dotnet test
```

今後、本番品質に近づける段階でテストプロジェクトを追加します。その後は `dotnet test` で自動テストを実行できるようにします。

## よく使うコマンドまとめ

```bash
# アプリを起動する
dotnet run --project TodoApi

# ビルドする
dotnet build TodoApi/TodoApi.csproj

# テストする
dotnet test
```

## 今回の実装で学ぶこと

- `MapGet`, `MapPost`, `MapPut`, `MapDelete` によるREST APIの作り方
- URLから値を受け取る方法
- JSONリクエストをC#の型として受け取る方法
- HTTPステータスコードの返し方
- `record` を使ったデータ構造の定義
- `List<T>` を使ったインメモリ保存

次のステップでは、コードを少しずつ分割し、テスト、バリデーション、データベース保存、エラーハンドリングなどを追加していきます。
