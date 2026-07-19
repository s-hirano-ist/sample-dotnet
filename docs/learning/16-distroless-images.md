# Distrolessコンテナを理解する

Distrolessイメージは、アプリケーションの実行に必要なものを中心に含め、シェルやパッケージマネージャーなどを省いたイメージです。

## 1. SDKと実行イメージを分ける

SDKは、アプリケーションのビルドに必要です。

```text
SDKイメージ
  ├── dotnet restore
  ├── dotnet build
  └── dotnet publish
```

一方、実行時には通常SDKは必要ありません。

```text
実行イメージ
  └── dotnet TodoApi.dll
```

そのため、マルチステージビルドで役割を分けます。

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# ビルド

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# 発行済みアプリを実行
```

## 2. Distrolessの特徴

Distroless系の実行イメージには、一般的に次の特徴があります。

- シェルがない
- パッケージマネージャーがない
- 余分なツールが少ない
- 攻撃対象を減らしやすい
- イメージサイズを小さくしやすい
- コンテナ内での調査方法が限られる

実行イメージの中で次のような操作はできません。

```dockerfile
RUN apt-get update
RUN apt-get install curl
```

## 3. Healthcheckの考え方

現在のComposeでは、コンテナ内の`curl`で`/health`を確認しています。

```yaml
healthcheck:
  test: ["CMD", "curl", "--fail", "http://localhost:8080/health"]
```

Distrolessイメージには`curl`がないため、この方法は使えません。

代わりに、次のような外部の仕組みで確認します。

```text
ロードバランサー -> GET /health
ECSのヘルスチェック -> GET /health
監視システム -> GET /health
```

アプリ自体は`/health`を提供し、コンテナの外側からHTTPで確認する考え方です。

## 4. SQLiteボリュームとの関係

Distrolessイメージは、非rootユーザーで実行されることがあります。

```text
コンテナ内のアプリユーザー
  ↓ 書き込み
/data/todo.db
```

ボリュームの所有者や書き込み権限が合わないと、SQLiteへ保存できません。

本番で複数コンテナを動かす場合は、SQLiteファイルを共有するより、RDSなどの共有DBを使う構成が一般的です。

## 5. Globalization

小さいイメージには、ICUやtzdataなどのGlobalization依存ファイルが含まれない場合があります。

アプリが日本語処理、タイムゾーン、カルチャ依存の日時・文字列処理を行う場合は、動作確認が必要です。

```xml
<PropertyGroup>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

この設定はGlobalizationを不変モードにしますが、日本語などのカルチャ依存処理が必要なアプリへ安易に設定してはいけません。

## 6. 現在の構成との比較

このプロジェクトの現在のDockerfileは、学習とCompose確認のしやすさを優先しています。

```text
SDKステージ       -> sdk:10.0
実行ステージ      -> aspnet:10.0
Healthcheck       -> コンテナ内のcurl
データベース      -> SQLite Named Volume
```

Distroless化すると、次の設計変更が必要になります。

```text
実行ステージ      -> Distroless/Chiseled系aspnet
Healthcheck       -> ECSやALBなど外部からHTTP確認
データベース      -> 共有DBを検討
コンテナ調査      -> ログ、メトリクス、デバッグ用一時コンテナを利用
```

## 練習問題

現在のDockerfileで、Distrolessイメージへそのまま変更できない部分を探してください。

確認するポイント:

- `apt-get`を使っている箇所
- `curl`を使っている箇所
- `/data`へSQLiteを書き込む権限
- コンテナ内のシェルに依存した調査方法
- `/health`を外部から確認する方法
