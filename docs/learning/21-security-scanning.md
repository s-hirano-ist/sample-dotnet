# セキュリティ自動検査を理解する

公開リポジトリでは、コードと依存パッケージを継続的に検査することが重要です。

## 1. CodeQL

CodeQLは、コードをデータベースのように解析して脆弱性のパターンを探す仕組みです。

```text
C#コード
  ↓
CodeQLデータベースを作成
  ↓
セキュリティクエリで解析
  ↓
GitHub Code Scanningへ結果を表示
```

このリポジトリでは、`.github/workflows/codeql.yml`でC#を解析します。

## 2. CodeQL Workflow

```yaml
- name: Initialize CodeQL
  uses: github/codeql-action/init@v4
  with:
    languages: csharp

- name: Autobuild
  uses: github/codeql-action/autobuild@v4

- name: Perform CodeQL Analysis
  uses: github/codeql-action/analyze@v4
```

`init`で解析を準備し、`autobuild`でC#プロジェクトをビルドし、`analyze`で結果をGitHubへ送ります。

## 3. Dependabot

Dependabotは、依存パッケージやGitHub Actionsの更新を確認し、Pull Requestを作成します。

このリポジトリでは、次を監視します。

- NuGetパッケージ
- GitHub Actions

## 4. 脆弱性対応の流れ

```text
Dependabotが更新を検出
  ↓
Pull Requestを作成
  ↓
CIのテストを実行
  ↓
CodeQLとレビューで確認
  ↓
問題がなければマージ
```

自動更新をそのまま本番へ反映するのではなく、テストとレビューを通すことが重要です。

## 5. 権限

CodeQL Workflowでは、必要な権限だけを指定しています。

```yaml
permissions:
  security-events: write
  packages: read
  actions: read
  contents: read
```

セキュリティ結果をアップロードするために`security-events: write`が必要です。

## 練習問題

次の問題が見つかったとき、どの仕組みが関係するか考えてみてください。

```text
NuGetパッケージに脆弱性が見つかった
C#コードの入力処理に脆弱性パターンがある
GitHub Actionsのバージョンが古い
```

確認するポイント:

- DependabotとCodeQLの役割の違い
- Workflowを定期実行する理由
- 自動検出後にテストとレビューが必要な理由
