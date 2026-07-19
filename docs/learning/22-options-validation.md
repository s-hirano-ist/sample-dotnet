# 設定をOptionsへ束ねて検証する

設定値を文字列キーで何度も読むと、キーの書き間違いや不正な値に気づきにくくなります。
Optionsパターンを使うと、設定をC#のクラスへ束ねて扱えます。

## 1. 設定をクラスへ束ねる

```csharp
public sealed class ApiKeyOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
```

`Authentication:ApiKey`の値が`ApiKeyOptions.ApiKey`へ入ります。

## 2. DIへ登録する

```csharp
builder.Services
    .AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection("Authentication"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey))
    .ValidateOnStart();
```

`Bind`は設定セクションをOptionsへ変換します。
`Validate`は設定値が正しいか確認します。
`ValidateOnStart`は、最初にOptionsを使うまで待たず、アプリ起動時に検証します。

## 3. なぜ起動時に検証するのか

APIキーが未設定のまま起動すると、アプリは動いているように見えても、保護されたAPIだけが失敗します。
起動時に失敗させると、デプロイやコンテナ起動の段階で設定ミスを検出できます。

```text
設定読み込み
  ↓
Optionsへ変換
  ↓
検証
  ↓
問題があれば起動失敗
```

## 4. DIからOptionsを受け取る

認証ハンドラーは`IOptions<ApiKeyOptions>`をコンストラクターで受け取ります。
これは、DIコンテナが登録済みのOptionsを渡しているという意味です。

## 練習問題

- `Bind`と`Validate`はそれぞれ何をしているか
- `ValidateOnStart`がない場合、検証はいつ行われるか
- 本番環境のAPIキーをどこから注入するべきか

## 5. レート制限設定も型へ束ねる

APIキー以外の設定も、Optionsへまとめて検証できます。

```csharp
public sealed class RateLimitOptions
{
    public string Store { get; set; } = "Memory";
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 10;
}
```

このAPIでは、`Store`が`Memory`または`Redis`であること、許可数と時間枠が1以上であることを起動時に確認します。
設定値を使うミドルウェアは、`IOptions<RateLimitOptions>`をDIから受け取ります。
