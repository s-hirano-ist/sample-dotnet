# DBプロバイダー切替とRepository境界

## 今回の目的

Todoの業務処理が、SQLiteやPostgreSQLの具体的なAPIへ直接依存しない構造にします。

ここでいう「切替可能」とは、TodoServiceのコードを書き換えずに、設定でDBプロバイダーを選べることです。

## 1. 設定からプロバイダーを選ぶ

`DatabaseOptions`は、設定ファイルの`Database`部分を受け取るC#クラスです。

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ApplyMigrations": false
  }
}
```

`AddTodoPersistence`はこの値を読み、`UseSqlite`または`UseNpgsql`を呼び分けます。設定値を環境変数で上書きすれば、同じアプリケーションを別のDBへ接続できます。

```bash
Database__Provider=Postgres
ConnectionStrings__TodoDatabase='Host=localhost;Port=5432;Database=todo;Username=todo;Password=todo'
```

`__`は、環境変数で設定階層を表す.NETの記法です。`Database__Provider`は`Database:Provider`として読み込まれます。

## 2. RepositoryでDB実装を隠す

`TodoService`が`TodoDbContext`を直接操作すると、業務処理とEF Coreの詳細が混ざります。

そこで、アプリケーション側に`ITodoRepository`インターフェースを置きます。インターフェースは「何ができるか」だけを定義し、`EfTodoRepository`が具体的なEF Core処理を担当します。

TodoServiceは`ITodoRepository`をコンストラクターで受け取るため、`DbContext`やSQLの詳細を知りません。DIコンテナが実行時に`EfTodoRepository`を渡します。

この境界があると、将来テスト用のFake実装や、別の保存方式を追加しやすくなります。ただし、Repositoryを増やせば必ずよいわけではありません。今回のようにDBアクセスと業務処理を分けて学ぶ目的がある場合に有効です。

## 3. SQLiteとPostgreSQLで初期化方法を分ける

テストは毎回新しいSQLite接続を作るため、`EnsureCreated()`でテーブルを準備します。これは学習用テストを速く、独立させるための選択です。

一方、本番相当のPostgreSQLでは、スキーマ変更の履歴をマイグレーションで管理します。Composeでは次の順番です。

1. PostgreSQLがhealthcheckを通過する
2. `TodoApi.Migrator`が`Database.MigrateAsync()`を実行する
3. マイグレーターが成功したらAPIコンテナを起動する

API本体の全インスタンスが同時にマイグレーションを実行すると、起動競合や権限分離の問題が起こり得ます。そのため、マイグレーションを専用ジョブに分けています。

## 4. なぜPostgreSQL用のマイグレーションなのか

EF Coreのマイグレーションは、プロバイダーごとにSQLの型やDDLが異なります。SQLite用に作ったマイグレーションを、そのままPostgreSQLの本番DBへ適用する設計にはしません。

今回のマイグレーションはPostgreSQLを基準に生成しています。SQLiteのテストではマイグレーションを使わず、SQLite用のEF Coreモデルからテーブルを作ります。

## 学習ポイント

- 設定値を使うと、コード変更なしで接続先を切り替えられる
- `UseSqlite`と`UseNpgsql`はEF CoreのDBプロバイダーを選ぶ設定
- `ITodoRepository`はアプリケーションとDB実装の境界
- DIはインターフェースと実装の対応を登録し、必要なクラスへ注入する
- SQLiteの`EnsureCreated`とPostgreSQLの`MigrateAsync`は目的が異なる
- ComposeではDB準備とAPI起動を分離できる
