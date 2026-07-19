# コンテナレジストリを理解する

Dockerfileで作ったイメージは、コンテナレジストリへ保存して別の実行環境から取得できます。

## 1. レジストリの役割

```text
Dockerfile
  ↓ build
コンテナイメージ
  ↓ push
コンテナレジストリ
  ↓ pull
ECSやKubernetes
```

開発者のPCでイメージを作り、実行環境がレジストリから同じイメージを取得します。

## 2. GHCR

GitHub Container Registryは、GitHubのリポジトリに関連付けてコンテナイメージを保存できます。

このリポジトリでは、次のWorkflowを追加しています。

```text
.github/workflows/publish-image.yml
```

## 3. 手動公開

イメージ公開Workflowは、意図しないpushを避けるため手動実行だけにしています。

```yaml
on:
  workflow_dispatch:
```

GitHubのActions画面からWorkflowを選択し、「Run workflow」を実行します。

## 4. GITHUB_TOKEN

Workflowは、GitHubが自動で用意する`GITHUB_TOKEN`を使ってGHCRへログインします。

```yaml
permissions:
  contents: read
  packages: write
```

`packages: write`は、コンテナパッケージをpushするための権限です。

権限は必要最小限に設定します。

## 5. イメージタグ

Workflowでは、次のタグを付けます。

```text
ghcr.io/s-hirano-ist/sample-dotnet:sha-xxxxx
ghcr.io/s-hirano-ist/sample-dotnet:latest
```

SHAタグは、どのコミットから作られたイメージかを追跡しやすいタグです。

`latest`は便利ですが、同じタグの中身が変わるため、本番環境ではSHAやリリースバージョンを固定する設計が安全です。

## 6. pushとpull

公開されたイメージは、次のようなコマンドで取得できます。

```bash
docker pull ghcr.io/s-hirano-ist/sample-dotnet:latest
```

ECSでは、タスク定義のコンテナイメージにレジストリのイメージURLを指定します。

```text
ghcr.io/s-hirano-ist/sample-dotnet:sha-xxxxx
```

## 練習問題

次の2つのタグの違いを説明してみてください。

```text
:latest
:sha-xxxxx
```

確認するポイント:

- どちらがイメージの内容を固定しやすいか
- ロールバック時にどちらが使いやすいか
- なぜWorkflowを手動実行にしているか
- `packages: write`が必要な理由
