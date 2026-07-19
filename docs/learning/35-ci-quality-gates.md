# CIで本番品質の変更を検証する

CIは、テストが通ることだけでなく、変更したスキーマやコンテナ設定が整合していることも確認します。

現在のCIでは次を実行します。

- `dotnet restore`
- `dotnet tool restore`
- Release構成の`dotnet build`
- EF Coreの`has-pending-model-changes`
- カバレッジ付き`dotnet test`
- `docker compose config --quiet`
- Dockerイメージのビルド

## マイグレーション検証

`TodoDbContext`を変更したのにマイグレーションを作り忘れると、開発環境では動いても本番DBへ変更が反映されません。`has-pending-model-changes`でモデルと最後のマイグレーションの差分を検出します。

## 品質ゲートの考え方

CIは、コードの正しさだけでなく、依存関係の復元、DBスキーマ、Compose設定、コンテナビルドを同じ変更単位で確認します。これにより、アプリだけではなく実行環境に関する壊れ方もPull Request段階で検出できます。
