// InMemoryTodoServiceは、Todoを操作する処理をまとめたクラスです。
// 今回は学習用なので、データベースではなくメモリ上のListにTodoを保存します。
public class InMemoryTodoService
{
    // private は、このクラスの中からだけ使えるという意味です。
    // 外から直接Listを書き換えられないようにしています。
    private readonly List<TodoItem> _todos = new();

    // 新しいTodoに割り当てるIDです。
    // nextId++ と書くと、今の値を使った後に1増えます。
    private int _nextId = 1;

    // IReadOnlyList<T> は「読み取り専用の一覧」を表す型です。
    // 呼び出し側に、Listを直接変更させない意図を表しています。
    public IReadOnlyList<TodoItem> GetAll()
    {
        return _todos;
    }

    public TodoItem? GetById(int id)
    {
        // FirstOrDefault は、条件に合う最初の要素を探します。
        // 見つからない場合は null を返します。
        return _todos.FirstOrDefault(todo => todo.Id == id);
    }

    public TodoItem Create(CreateTodoRequest request)
    {
        // record型は、new TodoItem(...) のように値を指定して作れます。
        var todo = new TodoItem(
            Id: _nextId++,
            Title: request.Title,
            IsDone: false,
            CreatedAt: DateTimeOffset.UtcNow,
            CompletedAt: null
        );

        _todos.Add(todo);

        return todo;
    }

    public TodoItem? Update(int id, UpdateTodoRequest request)
    {
        // FindIndex は、条件に合う要素の位置を返します。
        // 見つからない場合は -1 を返します。
        var index = _todos.FindIndex(todo => todo.Id == id);

        if (index == -1)
        {
            return null;
        }

        var existingTodo = _todos[index];

        // ?? は null合体演算子です。
        // 左側がnullでなければ左側、nullなら右側を使います。
        var isDone = request.IsDone ?? existingTodo.IsDone;

        // recordの with 式です。
        // existingTodoを元に、一部のプロパティだけ変えた新しいTodoを作ります。
        var updatedTodo = existingTodo with
        {
            Title = request.Title ?? existingTodo.Title,
            IsDone = isDone,
            // 完了状態なら完了日時を入れ、未完了なら null に戻します。
            CompletedAt = isDone
                ? existingTodo.CompletedAt ?? DateTimeOffset.UtcNow
                : null
        };

        // List内の古いTodoを、新しく作ったTodoで置き換えます。
        _todos[index] = updatedTodo;

        return updatedTodo;
    }

    public bool Delete(int id)
    {
        var todo = GetById(id);

        if (todo is null)
        {
            return false;
        }

        _todos.Remove(todo);

        return true;
    }
}
