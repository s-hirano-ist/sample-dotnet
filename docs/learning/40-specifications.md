# Specificationパターン

## 今回の目的

Todo一覧の完了状態フィルターとタイトル検索を、Repositoryの中に直接書かず、Specificationとして表現します。

Specificationは「ある条件を満たすか」を名前付きのオブジェクトとして扱うパターンです。

## 1. 条件をRepositoryから分離する

変更前は、Repositoryの`BuildFilteredQuery`に条件が直接書かれていました。

```csharp
if (isDone.HasValue)
{
    query = query.Where(todo => todo.IsDone == isDone.Value);
}
```

条件が増えるとRepositoryのメソッドが長くなり、同じ条件を別の検索で再利用しにくくなります。

変更後は、`TodoFilterSpecification`が条件を作ります。

```csharp
var specification = new TodoFilterSpecification(isDone, search);
var query = dbContext.Todos.Where(specification.Criteria);
```

## 2. Expressionとは

`Expression<Func<TodoItem, bool>>`は、Todoを受け取り、条件に一致するかを返す処理を「式」として保持する型です。

通常の関数と似ていますが、EF Coreは式の中身を読み取り、SQLの`WHERE`句へ変換できます。

```csharp
Expression<Func<TodoItem, bool>> criteria = todo => todo.IsDone;
```

`Func<TodoItem, bool>`として実行するとC#のメモリ上で評価されますが、Expressionとして`IQueryable`へ渡すと、DB側のSQL検索へ変換されます。

## 3. 各層の責務

- Query Use Case: 一覧取得という操作の流れを担当
- Specification: 完了状態やタイトル検索という条件を表現
- Repository: Specificationを`IQueryable`へ適用し、DBから取得
- EF Core: ExpressionをSQLへ変換

SpecificationはRepositoryそのものではありません。データを取得するのではなく、取得条件だけを持ちます。

## 4. Domain Serviceを追加しなかった理由

Domain Serviceは、複数のEntityやAggregateにまたがり、特定のEntityだけには置きにくいドメインルールに向いています。

現在のTodoには、そのようなルールがありません。検索条件を無理にDomain Serviceへ入れると、責務が不自然になるため、今回はSpecificationだけを導入します。

## 学習ポイント

- Specificationは条件を名前付きの型として扱う
- `Expression<Func<T, bool>>`はEF CoreがSQLへ変換できる
- 検索条件とDBアクセスを分離できる
- Domain Serviceは必要なルールがある場合だけ追加する
