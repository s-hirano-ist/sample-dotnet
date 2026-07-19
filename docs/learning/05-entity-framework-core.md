# Entity Framework Coreを理解する

Entity Framework Core（EF Core）は、C#のオブジェクトを使ってデータベースを操作するためのORMです。

ORMは、データベースのテーブルとプログラムのオブジェクトを対応付ける仕組みです。

## 1. Entityとは

`TodoItem`は、データベースの`Todos`テーブルの1行に対応するEntityです。

```csharp
public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsDone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
```

対応関係:

```text
C#                         データベース
TodoItem                   Todosテーブル
Id                         Id列
Title                      Title列
IsDone                     IsDone列
CreatedAt                  CreatedAt列
CompletedAt                CompletedAt列
```

## 2. DbContextとは

`TodoDbContext`は、アプリケーションとデータベースの接続窓口です。

```csharp
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> Todos => Set<TodoItem>();
}
```

`DbContext`には主に次の役割があります。

- DB接続の管理
- Entityとテーブルの対応管理
- SQLの生成と実行
- Entityの変更追跡

## 3. DbSetとは

```csharp
public DbSet<TodoItem> Todos => Set<TodoItem>();
```

`DbSet<TodoItem>`は、`TodoItem`を検索・追加・更新・削除するための入口です。

```csharp
var todos = await _dbContext.Todos.ToListAsync(cancellationToken);
```

これは次のSQLに近い処理をEF Coreへ依頼しています。

```sql
SELECT * FROM Todos;
```

## 4. LINQからSQLへ変換される

```csharp
var query = _dbContext.Todos
    .Where(todo => todo.IsDone == false)
    .OrderBy(todo => todo.Id)
    .Take(20);
```

この時点では、通常まだSQLは実行されていません。検索条件を組み立てた`IQueryable`を作っています。

```csharp
var todos = await query.ToListAsync(cancellationToken);
```

`ToListAsync`を呼んだときに、検索が実行されます。

現在の`TodoService`では、検索、件数取得、ページ取得をDB側で行っています。

```csharp
var query = _dbContext.Todos.AsNoTracking();

if (isDone.HasValue)
{
    query = query.Where(todo => todo.IsDone == isDone.Value);
}

var totalCount = await query.CountAsync(cancellationToken);
var todos = await query
    .OrderBy(todo => todo.Id)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);
```

## 5. 追加・更新・削除

Entityを追加するときは、`Add`と`SaveChangesAsync`を使います。

```csharp
_dbContext.Todos.Add(todo);
await _dbContext.SaveChangesAsync(cancellationToken);
```

`Add`だけではDBへ保存されません。`SaveChangesAsync`を呼んだ時点でINSERTが実行されます。

更新では、取得したEntityのプロパティを変更します。

```csharp
todo.IsDone = true;
await _dbContext.SaveChangesAsync(cancellationToken);
```

削除では、`Remove`を呼んでから保存します。

```csharp
_dbContext.Todos.Remove(todo);
await _dbContext.SaveChangesAsync(cancellationToken);
```

## 6. 変更追跡と`AsNoTracking`

通常、EF Coreは取得したEntityの状態を追跡します。

```csharp
var todo = await _dbContext.Todos.FirstOrDefaultAsync(todo => todo.Id == id);
todo.IsDone = true;
await _dbContext.SaveChangesAsync(cancellationToken);
```

取得後にプロパティが変更されたことをEF Coreが把握できるため、更新SQLを生成できます。

一覧表示のように変更しない読み取り処理では、`AsNoTracking`を使えます。

```csharp
var todos = await _dbContext.Todos
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

## 7. マイグレーション

Entityの定義を変更しただけでは、既存のテーブルは自動的に変わりません。

マイグレーションは、モデルの変更をデータベースへ反映するための履歴です。

```text
Entityの変更
  ↓
マイグレーションを作成
  ↓
データベースへ適用
```

このプロジェクトでは、起動時に次の処理を行っています。

```csharp
dbContext.Database.Migrate();
```

これにより、未適用のマイグレーションをSQLiteへ適用します。

## 練習問題

次の処理がDBへ何を依頼しているか説明してみてください。

```csharp
var todos = await _dbContext.Todos
    .AsNoTracking()
    .Where(todo => todo.IsDone == false)
    .OrderBy(todo => todo.Id)
    .Take(10)
    .ToListAsync(cancellationToken);
```

確認するポイント:

- `DbSet<TodoItem>`は何を表すか
- `Where`は何をするか
- `Take(10)`は何をするか
- `ToListAsync`を呼ぶまでSQLが実行されない理由
- `AsNoTracking`を使う場面
