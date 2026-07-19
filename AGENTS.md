# AGENTS.md

このリポジトリは、.NET初学者がTodo APIを題材にbackend開発を学ぶためのハンズオンです。

実装は一度に本番品質へ寄せず、小さい差分で動く状態を保ちながら段階的に改善します。説明コメントやREADMEは、初学者がC#とASP.NET Coreの基本文法を追えることを優先します。

## 現在の学習テーマ

現在は「OnStartingでエラー応答へ共通ヘッダーを付与する」ステップです。

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

今回のレート制限設定で学ぶこと:

- `appsettings.json`から数値設定を読み込む方法
- コードと設定値を分離する理由
- `Retry-After`ヘッダーで再試行時期を伝える方法
- HTTPステータスコードとレスポンスヘッダーを組み合わせる設計

今回のRedis分散レート制限で学ぶこと:

- インメモリ制限が複数コンテナで共有されない理由
- Redisを共有カウンターとして使う方法
- Luaスクリプトでカウンター更新と有効期限設定を原子的に行う考え方
- Redis障害時にレート制限を無効化しない設計

今回のRedisヘルスチェックで学ぶこと:

- 外部依存サービスの状態を監視する方法
- `IHealthCheck`を実装して独自のチェックを追加する方法
- DBだけでなくRedisも稼働条件に含める考え方
- 依存サービス障害時に`503 Service Unavailable`を返す運用設計

今回のRequest IDで学ぶこと:

- ASP.NET Coreのカスタムミドルウェアの基本
- `RequestDelegate`で次の処理へつなぐ方法
- `X-Request-Id`をレスポンスとログへ共通して付ける方法
- `BeginScope`でログへリクエスト情報を追加する方法
- クライアント入力をそのままログへ出さない考え方

今回のHTTPアクセスログで学ぶこと:

- `Stopwatch`で処理時間を測る方法
- `try`と`finally`で成功・失敗に関係なくログを残す方法
- HTTPメソッド、パス、ステータス、処理時間の構造化ログ
- ログへ出してよい情報と出してはいけない情報の判断

今回の例外処理で学ぶこと:

- カスタムミドルウェアで例外をまとめて処理する方法
- `try`、`catch`で例外を捕捉する基本
- `ProblemDetails`形式でAPIエラーを返す考え方
- 詳細な例外情報をログだけに残し、クライアントへ漏らさない設計
- レスポンス送信開始後は、エラー形式へ変更できない制約

今回のページングで学ぶこと:

- クエリ文字列からページ番号と件数を受け取る方法
- `Skip`と`Take`でデータベースから必要な範囲だけ取得する方法
- `Count`で全件数を別に取得する理由
- 一覧レスポンスへページ情報を含める設計
- ページ番号と最大件数をバリデーションする考え方

今回のフィルタリングで学ぶこと:

- クエリ文字列から任意の検索条件を受け取る方法
- `IQueryable`へ条件を追加してからSQLとして実行する流れ
- `Where`で完了状態に一致するTodoだけを取得する方法
- フィルター後の件数を基準にページングする考え方

今回のタイトル検索で学ぶこと:

- 文字列検索用のクエリパラメーターを受け取る方法
- `Trim`で入力の前後の空白を扱う方法
- `Contains`をEF CoreのDB検索へ変換する考え方
- 複数の検索条件を同じ`IQueryable`へ組み合わせる方法

今回のソートで学ぶこと:

- `OrderBy`と`OrderByDescending`でDB検索結果を並び替える方法
- `ThenBy`で同じ値の並び順を安定させる考え方
- 利用可能なソート項目をホワイトリストで制限する方法
- 外部入力をSQLへ直接渡さない設計

今回のキャンセル処理で学ぶこと:

- `CancellationToken`でリクエストの中断を通知する方法
- Minimal APIからサービス、EF Coreへキャンセルを伝播する方法
- `CountAsync`や`SaveChangesAsync`へトークンを渡す理由
- クライアント切断後の不要な処理を減らす考え方

今回のDocker Composeで学ぶこと:

- マルチステージDockerfileで.NETアプリをビルドする方法
- 実行イメージとSDKイメージを分ける理由
- ComposeでAPIとRedisを同時に起動する方法
- Composeの環境変数で設定値を注入する方法
- SQLiteのデータをNamed Volumeへ保存する考え方
- コンテナのhealthcheckと`depends_on`の関係

今回のDistrolessで学ぶこと:

- SDKイメージと実行イメージを分ける理由
- Distrolessイメージの特徴と制約
- コンテナ内ツールに依存しないヘルスチェック設計
- 非rootユーザーとボリューム権限の関係
- Globalization依存とイメージ選択

今回のコンテナ権限で学ぶこと:

- Dockerfileの`USER`で実行ユーザーを変更する方法
- `APP_UID`を使って非rootユーザーを指定する方法
- Named Volumeへ書き込むための所有権を準備する方法
- 既存ボリュームの所有権を初期化サービスで整える方法
- root権限でアプリを実行しない理由

今回のCIで学ぶこと:

- GitHub ActionsのWorkflow、Job、Stepの関係
- pushやPull Requestをトリガーにする方法
- CI上で`dotnet test`を実行する方法
- `docker compose config`で設定を検証する方法
- Dockerイメージをpushせずにビルド検証する方法
- `needs`でJobの実行順を制御する方法

今回のコードカバレッジで学ぶこと:

- Line、Branch、Method coverageの違い
- `coverlet.collector`でカバレッジを収集する方法
- GitHub ActionsのArtifactへ結果を保存する方法
- カバレッジ率とテスト品質を混同しない考え方

今回のコンテナレジストリで学ぶこと:

- コンテナレジストリの役割
- GHCRへイメージをpushする方法
- GitHub Actionsの`GITHUB_TOKEN`と権限設定
- SHAタグと`latest`タグの違い
- 本番でイメージタグを固定する理由

今回のECS on Fargateで学ぶこと:

- Task Definition、Task、Serviceの関係
- ALBからTaskへHTTPを転送する流れ
- ECSとALBのヘルスチェックの違い
- Taskが複数ある場合の状態共有
- ECSの環境変数とSecrets Managerの使い分け
- 複数TaskでRedisが必要になる理由

今回のセキュリティ検査で学ぶこと:

- CodeQLでC#コードを解析する方法
- DependabotでNuGetとGitHub Actionsを更新する方法
- GitHub Actionsの権限を最小限にする考え方
- 脆弱性検出、CI、レビューを組み合わせる流れ

今回のMiddleware順序改善で学ぶこと:

- Middlewareの登録順とログスコープの関係
- Request IDを例外ログまで伝播させる配置
- Middlewareを組み合わせた単体テスト

今回のキャンセル例外処理で学ぶこと:

- `OperationCanceledException`と通常の例外を分ける方法
- クライアント切断を`500`として扱わない理由
- `RequestAborted.IsCancellationRequested`の確認方法
- `catch`の順番と例外フィルターの基本

今回の認証テスト強化で学ぶこと:

- APIキーがない場合と間違っている場合を分けてテストする方法
- 認証失敗時にエンドポイントを実行させない考え方
- 固定時間比較を実装していても、異常系をテストで守る重要性

今回のOpenAPIメタデータで学ぶこと:

- `WithSummary`と`WithDescription`で仕様を説明する方法
- `Produces<T>`でレスポンス型をOpenAPIへ登録する方法
- ステータスコードごとのレスポンス契約を明示する考え方
- 実装だけでなく生成されたOpenAPI JSONをテストする方法

今回のOpenAPI認証定義で学ぶこと:

- `IOpenApiDocumentTransformer`の役割
- Security SchemeでAPIキー認証を表現する方法
- Swagger UIのAuthorize入力欄とAPI認証の関係
- 実装とAPI仕様書の認証方式を一致させる考え方

今回のセキュリティ自動検査で学ぶこと:

- CodeQLでC#コードの脆弱性パターンを検査する流れ
- DependabotでNuGetパッケージとGitHub Actionsの更新を確認する方法
- セキュリティ検査に必要なGitHub Actionsの権限
- 検査結果をテストとレビューで確認してから変更を取り込む考え方

今回のLivenessとReadinessで学ぶこと:

- プロセスの生存確認と依存サービスの準備確認を分ける理由
- `HealthCheckOptions.Predicate`で実行するチェックを絞る方法
- コンテナ再起動とロードバランサー切り離しで異なるURLを使う考え方
- 既存のヘルスチェックURLとの互換性を保ちながら改善する方法

今回のOptions設定検証で学ぶこと:

- 設定値をC#のOptionsクラスへ束ねる方法
- `Bind`で設定セクションを型へ変換する流れ
- `Validate`と`ValidateOnStart`で起動時に設定ミスを検出する方法
- 認証ハンドラーがDIからOptionsを受け取る仕組み
- 複数の設定項目をOptionsへまとめて検証する方法
- レート制限ミドルウェアが`IOptions<RateLimitOptions>`を受け取る流れ
- CORSのOrigin設定をOptionsへまとめて形式検証する方法
- OpenAPIのSecurity Schemeを各操作へ関連付ける方法
- エンドポイントメタデータから認証要否を判定する方法

今回のCORSプリフライトテストで学ぶこと:

- ブラウザが本来のリクエスト前に送る`OPTIONS`の意味
- `Access-Control-Request-Method`と`Access-Control-Request-Headers`の役割
- CORSレスポンスの許可Origin・メソッド・ヘッダーをテストする方法
- 許可していないOriginにCORSヘッダーを返さないことを確認する方法
- `AllowAnyMethod`や`AllowAnyHeader`を使わず許可範囲を明示する方法

今回のセキュリティヘッダーで学ぶこと:

- 共通Middlewareから全レスポンスへヘッダーを追加する方法
- `X-Content-Type-Options`、`X-Frame-Options`、`Referrer-Policy`の役割
- `Response.HasStarted`でヘッダー変更可能なタイミングを確認する方法
- セキュリティヘッダーを自動テストで守る考え方
- `OnStarting`で例外処理後のレスポンスにも共通ヘッダーを付ける考え方

今回のヘルスチェックレスポンスで学ぶこと:

- `HealthCheckOptions.ResponseWriter`でJSON形式を指定する方法
- 監視に必要な状態だけをレスポンスへ含める考え方
- 接続文字列や例外詳細をヘルスチェックから漏らさない設計

基礎学習の資料:

- `docs/learning/18-code-coverage.md`
- `docs/learning/21-security-scanning.md`
- `docs/learning/19-container-registry.md`
- `docs/learning/20-ecs-fargate.md`
- `docs/learning/21-security-scanning.md`

今回の基礎復習で学ぶこと:

- 普通のC#メソッドで引数を渡す方法
- Minimal APIのラムダ式とエンドポイントの関係
- URL、クエリ文字列、JSONボディから値が入る流れ
- DIコンテナからサービスが渡される流れ
- `CancellationToken`がASP.NET Coreから渡される仕組み

基礎学習の資料:

- `docs/learning/01-aspnet-core-parameters.md`
- `docs/learning/02-dependency-injection.md`
- `docs/learning/03-middleware-pipeline.md`
- `docs/learning/04-async-await.md`
- `docs/learning/05-entity-framework-core.md`
- `docs/learning/06-dotnet-testing.md`
- `docs/learning/07-configuration.md`
- `docs/learning/08-authentication-authorization.md`
- `docs/learning/09-api-errors.md`
- `docs/learning/10-openapi-swagger.md`
- `docs/learning/11-logging.md`
- `docs/learning/12-cors.md`
- `docs/learning/13-health-checks.md`
- `docs/learning/14-rate-limiting.md`
- `docs/learning/15-container-deployment.md`
- `docs/learning/16-distroless-images.md`
- `docs/learning/17-ci.md`
- `docs/learning/18-code-coverage.md`

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
- レート制限を追加する 完了（クライアント単位のパーティションを追加、設定値を外部化）
- Redisで分散レート制限を追加する 完了（Redisモードを任意で有効化）
- Redisのヘルスチェックを追加する 完了
- Request IDを追加する 完了
- HTTPアクセスログを追加する 完了
- 予期しない例外の共通処理を追加する 完了
- Todo一覧にページングを追加する 完了
- Todo一覧に完了状態フィルターを追加する 完了
- Todo一覧にタイトル検索を追加する 完了
- Todo一覧のソートを追加する 完了
- DB処理へCancellationTokenを伝播する 完了
- Docker ComposeでAPIとRedisを起動できるようにする 完了
- Distroless実行イメージの適用条件を整理する 完了
- APIコンテナを非rootユーザーで実行する 完了
- CIでテストとコンテナビルドを自動検証する 完了
- GHCRへコンテナイメージを手動公開できるようにする 完了
- Request IDを例外処理ログまで伝播させる 完了
- リクエストキャンセルを500へ変換しないようにする 完了
- 無効なAPIキーの異常系テストを追加する 完了
- OpenAPIへレスポンス契約を明示する 完了
- OpenAPIへAPIキー認証を明示する 完了
- CodeQLでC#コードを自動検査する 完了
- Dependabotで依存関係を定期確認する 完了
- Liveness用の`/live`とReadiness用の`/ready`を追加する 完了
- APIキー設定をOptionsへ移し、起動時検証を追加する 完了
- レート制限設定をOptionsへ移し、値を検証する 完了
- CORSのOrigin設定をOptionsへ移し、URL形式を検証する 完了
- CORSプリフライトリクエストをテストする 完了
- 許可していないOriginのCORSヘッダーをテストする 完了
- CORSの許可メソッドとヘッダーを設定で限定する 完了
- OpenAPIの認証要求を保護された操作へ関連付ける 完了
- HTTPメソッドに依存しないOpenAPI認証判定へ改善する 完了
- セキュリティヘッダーを共通Middlewareで追加する 完了
- HTTPS時だけHSTSヘッダーを追加する 完了
- `Permissions-Policy`で不要なブラウザ機能を無効にする 完了
- APIレスポンスへCSPを追加し、Swagger UIとの違いを扱う 完了
- `OnStarting`でエラー応答にもセキュリティヘッダーを付与する 完了
- ヘルスチェックレスポンスを安全なJSON形式へ統一する 完了
- OpenAPIへ共通ProblemDetailsの500レスポンスを登録する 完了
- 認証失敗時の401レスポンスをProblemDetails形式へ統一する 完了
- 認証方式名を定数へまとめて文字列重複を減らす 完了
- APIキーのHTTPヘッダー名を認証処理とOpenAPIで共有する 完了
- CORSとレート制限のポリシー名を定数へまとめる 完了
- APIキー認証を名前付き認可ポリシーへまとめる 完了
- 認可ポリシーで`permission` Claimを要求する 完了
- 認可失敗時の403レスポンスをProblemDetails形式へ統一する 完了
- 認可失敗の403レスポンスを単体テストで確認する 完了
- APIキーの権限Claimを設定から付与できるようにする 完了
- 読み取り専用APIキーで実際の403を統合テストする 完了
- 認証Claimの名前と権限値を定数へまとめる 完了
- APIキーのローテーション期間に追加キーを受け付ける 完了
- クライアントごとにAPIキーと権限を分ける 完了
- APIキー設定の必須項目と重複を起動時に検証する 完了
- APIキー設定のValidatorを専用クラスへ分離し単体テストする 完了
- CORSとレート制限のValidatorを専用クラスへ分離し単体テストする 完了
- 設定セクション名と接続文字列名を定数へまとめる 完了
- ローテーション用の追加キーが空でないことを起動時に検証する 完了
- APIキーを固定長ダイジェストへ変換して比較する 完了
- OpenAPI仕様書へタイトルとバージョンを追加する 完了
- 無効なAPIキーを値なしでWarningログへ記録する 完了

完了目安:

- API仕様をブラウザで確認できる
- 設定、ログ、セキュリティの基本方針を説明できる

## 開発方針

- 小さく動く状態を保つ
- 各ステップで `dotnet build` を通す
- テスト追加後は `dotnet test` を通す
- 学習用コメントは有用な間は残し、コード分割が進んだら必要に応じて整理する
- 本番品質化は、テストと責務分割を先に進めてから行う
