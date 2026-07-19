# .NETの設定を理解する

アプリケーションでは、環境によって変わる値をコードへ直接書かないことが重要です。

このプロジェクトでは、DB接続先、CORS、レート制限、APIキーなどを設定として扱っています。

## 1. `appsettings.json`

基本設定は`TodoApi/appsettings.json`にあります。

```json
{
  "ConnectionStrings": {
    "TodoDatabase": "Data Source=todo.db"
  },
  "RateLimit": {
    "Store": "Memory",
    "PermitLimit": 10,
    "WindowSeconds": 10
  }
}
```

JSONの階層は、設定キーのコロン区切りに対応します。

```text
ConnectionStrings:TodoDatabase
RateLimit:Store
RateLimit:PermitLimit
```

## 2. `IConfiguration`から読む

ASP.NET Coreでは、`builder.Configuration`から設定を読み取れます。

```csharp
var rateLimitStore = builder.Configuration.GetValue<string>("RateLimit:Store");
var permitLimit = builder.Configuration.GetValue<int>("RateLimit:PermitLimit", 10);
```

`GetValue<int>(キー, 10)`のようにデフォルト値を指定すると、設定がない場合の値を決められます。

接続文字列には専用のメソッドがあります。

```csharp
var connectionString = builder.Configuration
    .GetConnectionString("TodoDatabase");
```

これは`ConnectionStrings:TodoDatabase`を読み取ります。

## 3. 環境ごとの設定

ASP.NET Coreは環境名に対応した設定ファイルも読み込みます。

```text
appsettings.json
appsettings.Development.json
appsettings.Production.json
```

後から読み込まれた設定が、同じキーの値を上書きします。

```text
共通設定
  ↓
環境別設定で上書き
  ↓
環境変数などでさらに上書き
```

開発環境と本番環境で、DB接続先やログレベルを変えられます。

## 4. User Secrets

APIキーやパスワードを`appsettings.json`へ保存してはいけません。

ローカル開発では.NET User Secretsを使えます。

```bash
dotnet user-secrets set "Authentication:ApiKey" "dev-only-key" --project TodoApi
```

コードからは通常の設定と同じように読み取れます。

```csharp
var apiKey = builder.Configuration["Authentication:ApiKey"];
```

User Secretsはプロジェクトの外に保存されるため、Gitへコミットされません。

本番環境では、環境変数やクラウドのシークレット管理サービスを使います。

## 5. 環境変数

JSONのコロンは、環境変数では通常`__`（アンダースコア2つ）で表します。

```text
RateLimit:PermitLimit
```

```bash
RateLimit__PermitLimit=20
```

環境変数は、コンテナやECSなどの実行環境から設定値を注入する方法としてよく使われます。

## 6. 設定の優先順位

同じキーが複数の場所にある場合、一般的には後から読み込まれる設定が優先されます。

```text
appsettings.json
  ↓
appsettings.{Environment}.json
  ↓
User Secrets（開発環境）
  ↓
環境変数
  ↓
コマンドライン引数
```

そのため、コードを変えずに環境ごとの値を差し替えられます。

## 7. Optionsパターン

設定項目が増えると、文字列キーを何度も書くのは管理しにくくなります。

```csharp
var limit = builder.Configuration.GetValue<int>("RateLimit:PermitLimit");
var window = builder.Configuration.GetValue<int>("RateLimit:WindowSeconds");
```

Optionsパターンでは、設定をC#の型へまとめられます。

```csharp
public class RateLimitOptions
{
    public string Store { get; set; } = "Memory";
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 10;
}
```

```csharp
builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection("RateLimit")
);
```

サービスでは`IOptions<RateLimitOptions>`として受け取れます。

```csharp
public TodoService(IOptions<RateLimitOptions> options)
{
    var permitLimit = options.Value.PermitLimit;
}
```

設定値のまとまりを型として扱えるため、入力ミスを減らしやすくなります。

## 練習問題

次の設定値がどこから読まれるか説明してみてください。

```csharp
var permitLimit = builder.Configuration.GetValue<int>(
    "RateLimit:PermitLimit",
    10
);
```

確認するポイント:

- 対応する`appsettings.json`のキー
- 設定がない場合の値
- User Secretsや環境変数で上書きできる理由
- ECSで設定する場合の環境変数名
