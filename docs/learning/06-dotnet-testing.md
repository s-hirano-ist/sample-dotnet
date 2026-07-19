# .NETのテストを理解する

このプロジェクトでは、xUnitと`WebApplicationFactory`を使ってAPIをテストしています。

## 1. なぜテストを書くのか

テストは、コードを変更したあとも期待する動作が保たれているか確認するプログラムです。

```text
コードを変更
  ↓
dotnet test
  ↓
既存機能が壊れていないか確認
```

手動確認だけでは、毎回すべてのURLと入力を確認する必要があります。自動テストにすると、同じ確認を繰り返し実行できます。

## 2. Arrange・Act・Assert

テストは、次の3段階に分けると読みやすくなります。

```text
Arrange  テストの準備
Act      実際に処理を実行
Assert   結果を検証
```

現在のテストにもこの流れがあります。

```csharp
// Arrange: テスト用APIクライアントを作る
using var factory = new TodoApiTestFactory();
using var client = factory.CreateClient();

// Act: APIへリクエストを送る
var response = await client.GetAsync("/");

// Assert: レスポンスを検証する
response.EnsureSuccessStatusCode();
var body = await response.Content.ReadAsStringAsync();
Assert.Equal("Todo API is running.", body);
```

## 3. xUnitの`[Fact]`

`[Fact]`は、1つの固定されたテストケースを表します。

```csharp
[Fact]
public async Task GetRoot_ReturnsRunningMessage()
{
    // テスト処理
}
```

テストメソッドが例外なく終了し、すべての`Assert`が成功するとテスト成功です。

## 4. 単体テストと結合テスト

### 単体テスト

1つのクラスやメソッドを、他の部品から分離してテストします。

```text
TodoValidationだけをテスト
  ↓
DBやHTTPサーバーは起動しない
```

高速で、失敗した場所を特定しやすいのが特徴です。

### 結合テスト

複数の部品を組み合わせてテストします。

```text
HTTPリクエスト
  ↓
Minimal API
  ↓
TodoService
  ↓
EF Core
  ↓
SQLite
```

現在の`TodoApiTests`は、主にこの結合テストです。

## 5. `WebApplicationFactory`

`WebApplicationFactory<Program>`は、ASP.NET Coreアプリをテスト用に起動します。

```csharp
using var factory = new TodoApiTestFactory();
using var client = factory.CreateClient();
```

実際のポートを開かなくても、`HttpClient`からAPIへリクエストできます。

```csharp
var response = await client.GetAsync("/todos");
```

実際の利用者に近い形で、URL、HTTPメソッド、ステータスコード、JSONを確認できます。

## 6. テスト用DBを使う理由

テストで開発用の`todo.db`を使うと、テスト同士がデータを共有してしまいます。

現在のテストでは、SQLiteのインメモリDBを使っています。

```csharp
var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();
```

インメモリDBの特徴:

- テスト用に分離できる
- テスト終了後にデータが消える
- SQLiteのSQL実行に近い確認ができる
- 開発用DBを汚さない

## 7. HTTPレスポンスを検証する

APIテストでは、ステータスコードだけでなくレスポンスの内容も確認します。

```csharp
var response = await client.PostAsJsonAsync(
    "/todos",
    new { title = "Learn testing" }
);

Assert.Equal(HttpStatusCode.Created, response.StatusCode);
Assert.Equal("/todos/1", response.Headers.Location?.OriginalString);
```

エラーの場合も、ステータスコードとJSONの両方を確認できます。

```csharp
Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

var error = await response.Content.ReadFromJsonAsync<JsonObject>();
Assert.Equal("title_required", error?["code"]?.GetValue<string>());
```

## 8. テスト名

テスト名は、何をしたとき何が起きるかが分かる形にします。

```csharp
GetTodos_WhenNoTodoExists_ReturnsEmptyArray
PostTodos_WithBlankTitle_ReturnsBadRequest
GetTodos_WithSortByTitleDescending_ReturnsDescendingTitles
```

次の形で読むと意味を理解しやすくなります。

```text
対象_条件_期待結果
```

## 練習問題

次のテストをArrange・Act・Assertに分けて説明してみてください。

```csharp
using var factory = new TodoApiTestFactory();
using var client = factory.CreateClient();

var response = await client.GetAsync("/todos?pageSize=101");

Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
```

確認するポイント:

- テスト用APIを準備している行
- HTTPリクエストを送っている行
- 結果を検証している行
- このテストが守っているAPI仕様
