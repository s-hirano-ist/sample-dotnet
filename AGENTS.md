# AGENTS.md

このリポジトリは、.NET初学者がTodo APIを題材にbackend開発を学ぶためのハンズオンです。

実装は一度に本番品質へ寄せず、小さい差分で動く状態を保ちながら段階的に改善します。説明コメントやREADMEは、初学者がC#とASP.NET Coreの基本文法を追えることを優先します。

## 現在の学習テーマ

現在は「クライアント単位のレート制限を設計する」ステップです。

これまでの実装で学んだこと:

- `MapGet`, `MapPost`, `MapPut`, `MapDelete` によるREST APIの作り方
- URLから値を受け取る方法
- JSONリクエストをC#の型として受け取る方法
- HTTPステータスコードの返し方
- `record` を使ったデータ構造の定義
- `List<T>` を使ったインメモリ保存
- nullable型、`??`、`with`式、三項演算子などの基本構文

テスト追加で学んだこと:

- xUnitを使った.NETの自動テストの書き方
- `WebApplicationFactory<Program>` によるASP.NET Core APIのテスト起動
- `HttpClient` を使ったAPIテスト
- `Assert.Equal`, `Assert.NotNull`, `Assert.Empty` などの基本的な検証
- 正常系と異常系をテストで守る考え方

責務分割で学んだこと:

- `Program.cs` をHTTPの入口として薄く保つ考え方
- モデル、リクエスト型、サービスクラスの役割
- `public class` と `private` フィールドの基本
- `IReadOnlyList<T>` で読み取り専用の一覧を返す意図
- API層と業務ロジックを分ける理由

今回のバリデーションとエラー表現で学ぶこと:

- 入力チェックを専用クラスへまとめる考え方
- エラーを文字列だけでなく `code` と `message` を持つJSONとして返す理由
- `static class`、`const`、`static` プロパティの基本
- 作成時と更新時でバリデーション条件を少し変える設計
- 異常系テストでAPIの仕様を守る考え方

今回のデータベース保存で学ぶこと:

- SQLiteを使ったローカルDB保存
- Entity Framework Coreの基本
- Entity、DbContext、DbSetの役割
- マイグレーションでテーブル定義を管理する流れ
- アプリ本体はファイルDB、テストはインメモリSQLiteを使い分ける考え方

今回のOpenAPIで学ぶこと:

- OpenAPIがAPI仕様を機械可読なJSONとして表す仕組み
- `AddOpenApi`でOpenAPIサービスをDIコンテナへ登録する方法
- `MapOpenApi`で仕様書のHTTPエンドポイントを公開する方法
- `WithName`でエンドポイントに識別名を付ける考え方
- API仕様の公開自体も自動テストで確認する方法

今回のSwagger UIで学ぶこと:

- OpenAPI JSONとSwagger UIの役割の違い
- `UseSwaggerUI`で既存のOpenAPI仕様書を画面に表示する方法
- 開発環境だけで有効にする条件分岐
- APIドキュメントの画面も自動テストで確認する考え方

今回のログ出力で学ぶこと:

- `ILogger<T>`をDIから受け取る方法
- `LogInformation`と`LogWarning`の使い分け
- `{TodoId}`のようなプレースホルダーを使う構造化ログ
- `appsettings.json`でログレベルを設定する方法
- ログに入力内容を出さず、必要な情報だけを記録する考え方

今回のCORSで学ぶこと:

- ブラウザの同一オリジン制約とCORSの役割
- `AddCors`でCORSポリシーをDIへ登録する方法
- `UseCors`でHTTPリクエストへポリシーを適用する方法
- 許可するオリジンを`appsettings.json`で管理する方法
- 許可したOriginがレスポンスヘッダーへ入ることをテストする方法

今回の認証・認可で学ぶこと:

- 認証（誰かを確認する）と認可（何を許可するか）の違い
- `AddAuthentication`で認証方式を登録する方法
- `UseAuthentication`と`UseAuthorization`の役割
- `RequireAuthorization`で特定のエンドポイントを保護する方法
- `AuthenticationHandler`でHTTPヘッダーを確認する基本

今回の秘密情報管理で学ぶこと:

- 秘密情報を`appsettings.json`へ書かない理由
- `.NET User Secrets`をローカル開発で使う方法
- 設定値の優先順位と環境ごとの切り替え
- テストで本番の秘密情報を使わない方法
- 本番環境では環境変数やシークレット管理サービスを使う考え方

今回のヘルスチェックで学ぶこと:

- ヘルスチェックが運用監視で必要になる理由
- `AddHealthChecks`でチェック機能を登録する方法
- `AddDbContextCheck`でデータベース接続を検査する方法
- `MapHealthChecks`で監視用HTTPエンドポイントを公開する方法
- 正常時の`200`と異常時の`503`を使い分ける考え方

今回のレート制限で学ぶこと:

- レート制限が過剰アクセス対策になる理由
- `AddRateLimiter`で制限ルールを登録する方法
- 固定ウィンドウ方式と許可リクエスト数の考え方
- `UseRateLimiter`でミドルウェアを有効にする方法
- 制限超過時に`429 Too Many Requests`を返す設計
- `RateLimitPartition`でクライアントごとに制限状態を分ける方法
- 認証済みユーザーと未認証IPで制限キーを変える考え方
- インスタンス内の制限と分散レート制限の違い

現在のAPI入口は `TodoApi/Program.cs`、DB接続は `TodoApi/Data/TodoDbContext.cs`、Todo操作ロジックは `TodoApi/Services/TodoService.cs`、テストは `TodoApi.Tests/TodoApiTests.cs` にあります。

## 今後の拡張/学習計画

### 1. APIの基本を固める

- 現在のCRUD APIをcurlやHTTPクライアントで手動確認する
- 正常系と異常系のレスポンスを確認する
- HTTPメソッド、URL、ステータスコードの対応を理解する

完了目安:

- Todoの作成、一覧取得、1件取得、更新、削除を説明できる
- `200`, `201`, `204`, `400`, `404` の使い分けを説明できる

### 2. テストを追加する

- テストプロジェクトを追加する 完了
- APIの正常系テストを書く 完了
- 存在しないID、空タイトルなどの異常系テストを書く 完了
- 更新と削除のテストを追加する 完了
- テストが増えても読みやすい構成へ整理する

完了目安:

- `dotnet test` で自動テストが実行できる
- 手動curlだけに頼らず、既存機能を安全に変更できる

### 3. コードを責務ごとに分割する

- `Program.cs` からモデル、リクエスト型、Todo操作ロジックを分離する 完了
- Todo操作をサービスクラスに移す 完了
- エンドポイントはHTTPの入口として読みやすく保つ 完了

完了目安:

- API層と業務ロジックの違いを説明できる
- ファイルが増えても役割を追える

### 4. バリデーションとエラー表現を整える

- タイトルの必須チェックを整理する 完了
- タイトルの最大長などの制約を追加する 完了
- エラーレスポンスの形を統一する 完了

完了目安:

- 不正なリクエストに一貫したレスポンスを返せる
- 入力チェックの責務を説明できる

### 5. データベース保存に切り替える

- インメモリ保存からSQLiteなどの軽量DBへ移行する 完了
- Entity Framework Coreを導入する 完了
- マイグレーションでテーブルを作る 完了

完了目安:

- アプリ再起動後もTodoが残る
- Entity、DbContext、Migrationの役割を説明できる

### 6. 本番品質に近づける

- OpenAPI/Swaggerを追加する 完了（OpenAPI JSONと開発用Swagger UIを公開）
- ログ出力を確認する 完了
- 設定値を `appsettings.json` で管理する 完了（接続文字列・ログ・CORS設定）
- CORS、認証、認可を必要に応じて追加する（CORS完了、APIキー認証・認可完了）
- 秘密情報を安全に管理する（User Secrets導入完了）
- ヘルスチェックを追加する 完了
- レート制限を追加する 完了（クライアント単位のパーティションを追加）

完了目安:

- API仕様をブラウザで確認できる
- 設定、ログ、セキュリティの基本方針を説明できる

## 開発方針

- 小さく動く状態を保つ
- 各ステップで `dotnet build` を通す
- テスト追加後は `dotnet test` を通す
- 学習用コメントは有用な間は残し、コード分割が進んだら必要に応じて整理する
- 本番品質化は、テストと責務分割を先に進めてから行う
