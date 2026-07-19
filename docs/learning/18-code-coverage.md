# コードカバレッジを理解する

コードカバレッジは、テストがどの程度コードを実行したかを表す指標です。

## 1. カバレッジの種類

代表的な指標には次のようなものがあります。

```text
Line coverage       何行実行されたか
Branch coverage     ifなどの分岐を何通り実行したか
Method coverage     何個のメソッドが実行されたか
```

例えば次のコードがあります。

```csharp
if (isDone)
{
    return "done";
}

return "not done";
```

`isDone = true`だけをテストした場合、false側の分岐は実行されていません。

## 2. カバレッジは品質そのものではない

カバレッジが100%でも、テストの検証内容が弱ければ品質が高いとは限りません。

```csharp
var response = await client.GetAsync("/todos");
Assert.NotNull(response);
```

このテストはコードを実行するかもしれませんが、ステータスコードやレスポンス内容を十分に検証していません。

カバレッジは、テストされていない場所を見つける補助指標として使います。

## 3. Coverlet

このプロジェクトには`coverlet.collector`が入っています。

```xml
<PackageReference Include="coverlet.collector" Version="6.0.4" />
```

テスト実行時に次のオプションを付けると、カバレッジを収集できます。

```bash
dotnet test \
  --results-directory TestResults \
  --collect:"XPlat Code Coverage"
```

## 4. CIで保存する

GitHub Actionsでは、テスト後にカバレッジファイルをArtifactとして保存します。

```yaml
- name: Upload coverage report
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: code-coverage
    path: "**/TestResults/**/coverage.cobertura.xml"
```

Artifactは、Workflow実行後にGitHub画面からダウンロードできます。

`if: always()`を付けると、テストが失敗した場合でも結果ファイルが存在する限り保存を試みます。

## 5. 目標値の考え方

すべてのコードで一律に高いカバレッジを目指す必要はありません。

優先してテストする場所:

- 重要な業務ロジック
- 認証・認可
- 入力バリデーション
- エラー処理
- データの作成・更新・削除

単純な設定コードやフレームワーク呼び出しだけのコードは、同じ割合でテストする必要がない場合もあります。

## 練習問題

次の分岐に対して、必要なテストケースを考えてみてください。

```csharp
if (!validation.IsValid)
{
    return Results.BadRequest(validation.Error);
}

return Results.Created(/* ... */);
```

確認するポイント:

- 正常系のテスト
- バリデーション失敗のテスト
- それぞれのHTTPステータスコード
- カバレッジとテスト品質の違い
